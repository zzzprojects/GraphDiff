using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Reflection;

namespace RefactorThis.GraphDiff.Internal.Members
{
    internal abstract class AMember
    {
        internal AMember Parent { get; private set; }
        internal Stack<AMember> Members { get; private set; }
        
        protected readonly PropertyInfo Accessor;

        internal string IncludeString
        {
            get
            {
                var ownIncludeString = Accessor != null ? Accessor.Name : null;
                return Parent != null && Parent.IncludeString != null
                        ? Parent.IncludeString + "." + ownIncludeString
                        : ownIncludeString;
            }
        }

        protected AMember(AMember parent, PropertyInfo accessor)
        {
            Accessor = accessor;
            Members = new Stack<AMember>();
            Parent = parent;
        }

        protected T GetValue<T>(object instance)
        {
            return (T)Accessor.GetValue(instance, null);
        }

        protected void SetValue(object instance, object value)
        {
            Accessor.SetValue(instance, value, null);
        }

        internal abstract void Update<T>(DbContext context, T existing, T entity) where T : class, new();

        protected static string CreateHash(IEnumerable<PropertyInfo> keys, object entity)
        {
            // Create unique string representing the keys
            string code = "";
            foreach (var property in keys)
                code += "|" + property.GetValue(entity, null).GetHashCode();
            return code;
        }

        protected static List<PropertyInfo> GetPrimaryKeyFieldsFor(IObjectContextAdapter db, Type entityType)
        {
            var keyMembers = db.ObjectContext.MetadataWorkspace
                    .GetItems<EntityType>(DataSpace.OSpace)
                    .Single(p => p.FullName == entityType.FullName)
                    .KeyMembers;

            return keyMembers.Select(k => entityType.GetProperty(k.Name)).ToList();
        }

        protected static void AttachCyclicNavigationProperty(IObjectContextAdapter context, object parent, object child)
        {
            if (parent == null || child == null)
                return;

            var parentType = ObjectContext.GetObjectType(parent.GetType());
            var childType = ObjectContext.GetObjectType(child.GetType());

            var navigationProperties = context.ObjectContext.MetadataWorkspace
                    .GetItems<EntityType>(DataSpace.OSpace)
                    .Single(p => p.FullName == childType.FullName)
                    .NavigationProperties;

            var parentNavigationProperty = navigationProperties
                    .Where(navigation => navigation.TypeUsage.EdmType.Name == parentType.Name)
                    .Select(navigation => childType.GetProperty(navigation.Name))
                    .FirstOrDefault();

            if (parentNavigationProperty != null)
                parentNavigationProperty.SetValue(child, parent, null);
        }

        protected static void UpdateValuesWithConcurrencyCheck<T>(DbContext context, T from, T to) where T : class
        {
            EnsureConcurrency(context, from, to);
            context.Entry(to).CurrentValues.SetValues(from);
        }

        protected static void EnsureConcurrency<T>(IObjectContextAdapter db, T from, T to)
        {
            // get concurrency properties of T
            var entityType = ObjectContext.GetObjectType(@from.GetType());
            var metadata = db.ObjectContext.MetadataWorkspace;

            var objType = metadata.GetItems<EntityType>(DataSpace.OSpace).Single(p => p.FullName == entityType.FullName);

            // need internal string, code smells bad.. any better way to do this?
            var cTypeName = (string) objType.GetType()
                    .GetProperty("CSpaceTypeName", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(objType, null);

            var conceptualType = metadata.GetItems<EntityType>(DataSpace.CSpace).Single(p => p.FullName == cTypeName);
            var concurrencyProperties = conceptualType.Members
                    .Where(member => member.TypeUsage.Facets.Any(facet => facet.Name == "ConcurrencyMode" && (ConcurrencyMode)facet.Value == ConcurrencyMode.Fixed))
                    .Select(member => entityType.GetProperty(member.Name))
                    .ToList();

            // Check if concurrency properties are equal
            // TODO EF should do this automatically should it not?
            foreach (PropertyInfo concurrencyProp in concurrencyProperties)
            {
                // if is byte[] use array comparison, else equals().
                if ((concurrencyProp.PropertyType == typeof(byte[]) && !((byte[])concurrencyProp.GetValue(@from, null)).SequenceEqual((byte[])concurrencyProp.GetValue(to, null)))
                    || concurrencyProp.GetValue(@from, null).Equals(concurrencyProp.GetValue(to, null)))
                {
                    throw new DbUpdateConcurrencyException(String.Format("{0} failed optimistic concurrency", concurrencyProp.Name));
                }
            }
        }

        protected static void AttachAndReloadEntity(DbContext context, object entity)
        {
            if (context.Entry(entity).State == EntityState.Detached)
                context.Set(ObjectContext.GetObjectType(entity.GetType())).Attach(entity);

            if (GraphDiffConfiguration.ReloadAssociatedEntitiesWhenAttached)
                context.Entry(entity).Reload();
        }
    }
}
