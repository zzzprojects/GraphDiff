using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Metadata.Edm;
using System.Data.Objects;
using System.Linq;
using System.Reflection;

namespace RefactorThis.GraphDiff.Internal.Graph
{
    internal class GraphNode
    {
        #region Fields, Properties Constructors

        public GraphNode Parent { get; private set; }
        public Stack<GraphNode> Members { get; private set; }
        
        protected readonly PropertyInfo Accessor;

        public string IncludeString
        {
            get
            {
                var ownIncludeString = Accessor != null ? Accessor.Name : null;
                return Parent != null && Parent.IncludeString != null
                        ? Parent.IncludeString + "." + ownIncludeString
                        : ownIncludeString;
            }
        }

        public GraphNode()
        {
            Members = new Stack<GraphNode>();
        }

        public GraphNode(GraphNode parent, PropertyInfo accessor)
        {
            Accessor = accessor;
            Members = new Stack<GraphNode>();
            Parent = parent;
        }

        #endregion

        // overridden by different implementations
        public virtual void Update<T>(DbContext context, T persisted, T updating) where T : class, new()
        {
            UpdateValuesWithConcurrencyCheck(context, updating, persisted);

            // Foreach branch perform recursive update
            foreach (var member in Members)
                member.Update(context, persisted, updating);
        }

        protected T GetValue<T>(object instance)
        {
            return (T)Accessor.GetValue(instance, null);
        }

        protected void SetValue(object instance, object value)
        {
            Accessor.SetValue(instance, value, null);
        }

        protected static string CreateHashKey(IEnumerable<PropertyInfo> keys, object entity)
        {
            // Create unique string representing the keys
            return keys.Aggregate("", (current, property) => current + ("|" + property.GetValue(entity, null).GetHashCode()));
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
            if (context.Entry(to).State != EntityState.Added)
                EnsureConcurrency(context, from, to);

            context.Entry(to).CurrentValues.SetValues(from);
        }

        protected static void AttachAndReloadEntity(DbContext context, object entity)
        {
            if (context.Entry(entity).State == EntityState.Detached)
                context.Set(ObjectContext.GetObjectType(entity.GetType())).Attach(entity);

            if (GraphDiffConfiguration.ReloadAssociatedEntitiesWhenAttached)
                context.Entry(entity).Reload();
        }

        protected static bool IsKeyIdentical(IObjectContextAdapter context, object newValue, object dbValue)
        {
            if (newValue == null || dbValue == null)
                return false;

            var keyFields = context.GetPrimaryKeyFieldsFor(ObjectContext.GetObjectType(newValue.GetType()));
            return CreateHashKey(keyFields, newValue) == CreateHashKey(keyFields, dbValue);
        }

        private static void EnsureConcurrency<T>(IObjectContextAdapter db, T entity1, T entity2)
        {
            // get concurrency properties of T
            var entityType = ObjectContext.GetObjectType(entity1.GetType());
            var metadata = db.ObjectContext.MetadataWorkspace;

            var objType = metadata.GetItems<EntityType>(DataSpace.OSpace).Single(p => p.FullName == entityType.FullName);

            // need internal string, code smells bad.. any better way to do this?
            var cTypeName = (string)objType.GetType()
                    .GetProperty("CSpaceTypeName", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(objType, null);

            var conceptualType = metadata.GetItems<EntityType>(DataSpace.CSpace).Single(p => p.FullName == cTypeName);
            var concurrencyProperties = conceptualType.Members
                    .Where(member => member.TypeUsage.Facets.Any(facet => facet.Name == "ConcurrencyMode" && (ConcurrencyMode)facet.Value == ConcurrencyMode.Fixed))
                    .Select(member => entityType.GetProperty(member.Name))
                    .ToList();

            // Check if concurrency properties are equal
            // TODO EF should do this automatically should it not?
            foreach (var concurrencyProp in concurrencyProperties)
            {
                // if is byte[] use array comparison, else equals().

                var type = concurrencyProp.PropertyType;
                var obj1 = concurrencyProp.GetValue(entity1, null);
                var obj2 = concurrencyProp.GetValue(entity2, null);

                if (
                    (obj1 == null || obj2 == null) ||
                    (type == typeof (byte[]) && !((byte[]) obj1).SequenceEqual((byte[]) obj2)) ||
                    (type != typeof (byte[]) && !obj1.Equals(obj2))
                    )
                {
                    throw new DbUpdateConcurrencyException(String.Format("{0} failed optimistic concurrency", concurrencyProp.Name));
                }
            }
        }
    }
}
