using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Reflection;

namespace RefactorThis.GraphDiff.Internal.Graph
{
    internal class GraphNode
    {
        #region Fields, Properties Constructors

        public GraphNode Parent { get; private set; }
        public Stack<GraphNode> Members { get; private set; }
        public bool? AllowDelete { get; set; }

        protected readonly PropertyInfo Accessor;

        internal PropertyInfo AccessorCyclicNavigationProperty;

        protected string IncludeString
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

        protected GraphNode(GraphNode parent, PropertyInfo accessor)
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
            {
                member.Update(context, persisted, updating);
            }
        }

        protected T GetValue<T>(object instance)
        {
            return (T)Accessor.GetValue(instance, null);
        }

        protected void SetValue(object instance, object value)
        {
            Accessor.SetValue(instance, value, null);
        }

        protected static EntityKey CreateEntityKey(IObjectContextAdapter context, object entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }

            return context.ObjectContext.CreateEntityKey(context.GetEntitySetName(ObjectContext.GetObjectType(entity.GetType())), entity);
        }

        internal void GetIncludeStrings(DbContext context, List<string> includeStrings)
        {
            var ownIncludeString = IncludeString;
            if (!string.IsNullOrEmpty(ownIncludeString))
            {
                includeStrings.Add(ownIncludeString);
            }

            includeStrings.AddRange(GetRequiredNavigationPropertyIncludes(context));

            foreach (var member in Members)
            {
                member.GetIncludeStrings(context, includeStrings);
            }
        }

        protected virtual IEnumerable<string> GetRequiredNavigationPropertyIncludes(DbContext context)
        {
            return new string[0];
        }

        protected static IEnumerable<string> GetRequiredNavigationPropertyIncludes(DbContext context, Type entityType, string ownIncludeString)
        {
            return context.GetRequiredNavigationPropertiesForType(entityType)
                    .Select(navigationProperty => ownIncludeString + "." + navigationProperty.Name);
        }

        protected static void AttachCyclicNavigationProperty(IObjectContextAdapter context, object parent, object child, PropertyInfo parentNavigationProperty = null)
        {
            if (parent == null || child == null) return;

            var parentType = ObjectContext.GetObjectType(parent.GetType());
            var childType = ObjectContext.GetObjectType(child.GetType());

            var navigationProperties = context.GetNavigationPropertiesForType(childType);

            if (parentNavigationProperty == null)
			{
                // IF not parent property is specified, we take the first one if we found something
                parentNavigationProperty = navigationProperties
                    .Where(navigation => navigation.TypeUsage.EdmType.Name == parentType.Name)
                    .Select(navigation => childType.GetProperty(navigation.Name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                    .FirstOrDefault();
            }

            if (parentNavigationProperty != null)
                parentNavigationProperty.SetValue(child, parent, null);
        }

        protected static void UpdateValuesWithConcurrencyCheck<T>(DbContext context, T from, T to) where T : class
        {
            if (context.Entry(to).State != EntityState.Added)
            {
                EnsureConcurrency(context, from, to);
            }

            context.Entry(to).CurrentValues.SetValues(from);
        }

        protected static object AttachAndReloadAssociatedEntity(DbContext context, object entity)
        {
            var localCopy = FindLocalByKey(context, entity);
            if (localCopy != null) return localCopy;

            if (context.Entry(entity).State == EntityState.Detached)
            {
                var entityType = ObjectContext.GetObjectType(entity.GetType());
                var instance = CreateEmptyEntityWithKey(context, entity);
                
                context.Set(entityType).Attach(instance);
                context.Entry(instance).Reload();

                AttachRequiredNavigationProperties(context, entity, instance);
                return instance;
            }

            if (GraphDiffConfiguration.ReloadAssociatedEntitiesWhenAttached)
            {
                context.Entry(entity).Reload();
            }

            return entity;
        }

        private static object FindLocalByKey(DbContext context, object entity)
        {
            var eType = ObjectContext.GetObjectType(entity.GetType());
            return context.Set(eType).Local.OfType<object>().FirstOrDefault(local => IsKeyIdentical(context, local, entity));
        }

        protected static void AttachRequiredNavigationProperties(DbContext context, object updating, object persisted)
        {
            var entityType = ObjectContext.GetObjectType(updating.GetType());
            foreach (var navigationProperty in context.GetRequiredNavigationPropertiesForType(updating.GetType()))
            {
                var navigationPropertyInfo = entityType.GetProperty(navigationProperty.Name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                var associatedEntity = navigationPropertyInfo.GetValue(updating, null);
                if (associatedEntity != null)
                {
                    associatedEntity = FindEntityByKey(context, associatedEntity);
                }

                navigationPropertyInfo.SetValue(persisted, associatedEntity, null);
            }
        }

        private static object FindEntityByKey(DbContext context, object associatedEntity)
        {
            var associatedEntityType = ObjectContext.GetObjectType(associatedEntity.GetType());
            var keyFields = context.GetPrimaryKeyFieldsFor(associatedEntityType);
            var keys = keyFields.Select(key => key.GetValue(associatedEntity, null)).ToArray();
            return context.Set(associatedEntityType).Find(keys);
        }

        protected static object CreateEmptyEntityWithKey(IObjectContextAdapter context, object entity)
        {
            var instance = Activator.CreateInstance(ObjectContext.GetObjectType(entity.GetType()));
            CopyPrimaryKeyFields(context, entity, instance);
            return instance;
        }

        private static void CopyPrimaryKeyFields(IObjectContextAdapter context, object from, object to)
        {
            var keyProperties = context.GetPrimaryKeyFieldsFor(from.GetType()).ToList();
            foreach (var keyProperty in keyProperties)
                keyProperty.SetValue(to, keyProperty.GetValue(from, null), null);
        }

        protected static bool IsKeyIdentical(DbContext context, object newValue, object dbValue)
        {
            if (newValue == null || dbValue == null) return false;

            return CreateEntityKey(context, newValue) == CreateEntityKey(context, dbValue);
        }

        private static void EnsureConcurrency<T>(IObjectContextAdapter db, T entity1, T entity2)
        {
            // get concurrency properties of T
            var entityType = ObjectContext.GetObjectType(entity1.GetType());
            var metadata = db.ObjectContext.MetadataWorkspace;

            var objType = metadata.GetEntityTypeByType(entityType);

            // need internal string, code smells bad.. any better way to do this?
            var cTypeName = (string)objType.GetType()
                    .GetProperty("CSpaceTypeName", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(objType, null);

            var conceptualType = metadata.GetItems<EntityType>(DataSpace.CSpace).Single(p => p.FullName == cTypeName);
            var concurrencyProperties = conceptualType.Members
                    .Where(member => member.TypeUsage.Facets.Any(facet => facet.Name == "ConcurrencyMode" && (ConcurrencyMode)facet.Value == ConcurrencyMode.Fixed))
                    .Select(member => entityType.GetProperty(member.Name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
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
