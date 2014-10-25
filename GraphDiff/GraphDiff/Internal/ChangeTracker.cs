using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Reflection;

namespace RefactorThis.GraphDiff.Internal.Graph
{
    /// <summary>
    /// Change tracker abstraction
    /// </summary>
    internal interface IChangeTracker
    {
        /// <summary>
        /// Adds a new entity to the change tracker
        /// </summary>
        /// <param name="entity">The new entity</param>
        void AddItem(object entity);

        /// <summary>
        /// Updates the values of an existing tracked entity
        /// </summary>
        /// <param name="from">The old item</param>
        /// <param name="to">The new item values to apply</param>
        /// <param name="doConcurrencyCheck">Perform a concurrency check when updating</param>
        void UpdateItem(object from, object to, bool doConcurrencyCheck = false);

        /// <summary>
        /// Marks an entity as requiring removal from the database
        /// </summary>
        /// <param name="entity">The entity to be removed</param>
        void RemoveItem(object entity);

        /// <summary>
        /// Returns the current state of an entity (detached, attached, etc)
        /// </summary>
        EntityState GetItemState(object entity);

        /// <summary>
        /// Attach the associated entity to the change tracker and reload the entity.
        /// </summary>
        object AttachAndReloadAssociatedEntity(object entity);

        /// <summary>
        /// Ensure references back to the parent from the child are kept in sync
        /// </summary>
        void AttachCyclicNavigationProperty(object parent, object child);

        /// <summary>
        /// Ensures all required navigation properties are attached
        /// </summary>
        void AttachRequiredNavigationProperties(object updating, object persisted);
    }

    internal class ChangeTracker : IChangeTracker
    {
        private readonly DbContext _context;
        private readonly IEntityManager _entityManager;

        private ObjectContext _objectContext
        {
            get { return ((IObjectContextAdapter)_context).ObjectContext; }
        }

        public ChangeTracker(DbContext context, IEntityManager entityManager)
        {
            _entityManager = entityManager;
            _context = context;
        }

        public void AddItem(object item)
        {
            var type = ObjectContext.GetObjectType(item.GetType());
            _context.Set(type).Add(item);
        }

        public EntityState GetItemState(object item)
        {
            return _context.Entry(item).State;
        }

        public void UpdateItem(object from, object to, bool doConcurrencyCheck = false)
        {
            if (doConcurrencyCheck && _context.Entry(to).State != EntityState.Added)
            {
                EnsureConcurrency(from, to);
            }

            var keyProperties = GetComplexTypePropertiesForType(ObjectContext.GetObjectType(from.GetType()));
            foreach (var keyProperty in keyProperties)
            {
                keyProperty.SetValue(to, keyProperty.GetValue(from, null), null);
            }

            _context.Entry(to).CurrentValues.SetValues(from);
        }

        public void RemoveItem(object item)
        {
            var type = ObjectContext.GetObjectType(item.GetType());
            _context.Set(type).Remove(item);
        }

        public void AttachCyclicNavigationProperty(object parent, object child)
        {
            if (parent == null || child == null)
            {
                return;
            }

            var parentType = ObjectContext.GetObjectType(parent.GetType());
            var childType = ObjectContext.GetObjectType(child.GetType());

            var navigationProperties = _entityManager.GetNavigationPropertiesForType(childType);

            var parentNavigationProperty = navigationProperties
                    .Where(navigation => navigation.TypeUsage.EdmType.Name == parentType.Name)
                    .Select(navigation => childType.GetProperty(navigation.Name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                    .FirstOrDefault();

            if (parentNavigationProperty != null)
            {
                parentNavigationProperty.SetValue(child, parent, null);
            }
        }

        public object AttachAndReloadAssociatedEntity(object entity)
        {
            var localCopy = FindTrackedEntity(entity);
            if (localCopy != null)
            {
                return localCopy;
            }

            if (_context.Entry(entity).State == EntityState.Detached)
            {
                // TODO look into a possible better way of doing this, I don't particularly like it
                // will add a key-only object to the change tracker. at the moment this is being reloaded,
                // performing a db query which would impact performance
                var entityType = ObjectContext.GetObjectType(entity.GetType());
                var instance = _entityManager.CreateEmptyEntityWithKey(entity);

                _context.Set(entityType).Attach(instance);
                _context.Entry(instance).Reload();

                AttachRequiredNavigationProperties(entity, instance);
                return instance;
            }

            if (GraphDiffConfiguration.ReloadAssociatedEntitiesWhenAttached)
            {
                _context.Entry(entity).Reload();
            }

            return entity;
        }

        public void AttachRequiredNavigationProperties(object updating, object persisted)
        {
            var entityType = ObjectContext.GetObjectType(updating.GetType());
            foreach (var navigationProperty in _entityManager.GetRequiredNavigationPropertiesForType(updating.GetType()))
            {
                var navigationPropertyInfo = entityType.GetProperty(navigationProperty.Name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                var associatedEntity = navigationPropertyInfo.GetValue(updating, null);

                if (associatedEntity != null)
                {
                    // TODO this is performing a db query - look for alternative.
                    associatedEntity = FindEntityByKey(associatedEntity);
                }

                navigationPropertyInfo.SetValue(persisted, associatedEntity, null);
            }
        }

        // Privates

        private void EnsureConcurrency(object entity1, object entity2)
        {
            // get concurrency properties of T
            var entityType = ObjectContext.GetObjectType(entity1.GetType());
            var metadata = _objectContext.MetadataWorkspace;

            var objType = metadata.GetItems<EntityType>(DataSpace.OSpace).Single(p => p.FullName == entityType.FullName);

            // TODO need internal string, code smells bad.. any better way to do this?
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
                var type = concurrencyProp.PropertyType;
                var obj1 = concurrencyProp.GetValue(entity1, null);
                var obj2 = concurrencyProp.GetValue(entity2, null);

                // if is byte[] use array comparison, else equals().
                if (
                    (obj1 == null || obj2 == null) ||
                    (type == typeof(byte[]) && !((byte[])obj1).SequenceEqual((byte[])obj2)) ||
                    (type != typeof(byte[]) && !obj1.Equals(obj2))
                    )
                {
                    throw new DbUpdateConcurrencyException(String.Format("{0} failed optimistic concurrency", concurrencyProp.Name));
                }
            }
        }

        private object FindTrackedEntity(object entity)
        {
            var eType = ObjectContext.GetObjectType(entity.GetType());
            return _context.Set(eType)
                .Local
                .OfType<object>()
                .FirstOrDefault(local => _entityManager.AreKeysIdentical(local, entity));
        }

        private object FindEntityByKey(object associatedEntity)
        {
            var associatedEntityType = ObjectContext.GetObjectType(associatedEntity.GetType());
            var keyFields = _entityManager.GetPrimaryKeyFieldsFor(associatedEntityType);
            var keys = keyFields.Select(key => key.GetValue(associatedEntity, null)).ToArray();
            return _context.Set(associatedEntityType).Find(keys);
        }

        private IEnumerable<PropertyInfo> GetComplexTypePropertiesForType(Type entityType)
        {
            return _objectContext.MetadataWorkspace
                    .GetItems<EntityType>(DataSpace.OSpace)
                    .Single(p => p.FullName == entityType.FullName).Properties
                    .Where(p => p.IsComplexType)
                    .Select(k => entityType.GetProperty(k.Name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                    .ToList();
        }
    }
}
