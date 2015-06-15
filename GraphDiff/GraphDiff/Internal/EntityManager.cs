using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Reflection;

namespace RefactorThis.GraphDiff.Internal
{
    /// <summary>Entity creation, type & key management</summary>
    internal interface IEntityManager
    {
        /// <summary>Creates the unique entity key for an entity</summary>
        EntityKey CreateEntityKey(object entity);

        /// <summary>Creates an empty object of the same type and keys matching the entity provided</summary>
        object CreateEmptyEntityWithKey(object entity);

        /// <summary>Returns true if the keys of entity1 and entity2 match.</summary>
        bool AreKeysIdentical(object entity1, object entity2);

        /// <summary>Returns the primary key fields for a given  entity type</summary>
        IEnumerable<PropertyInfo> GetPrimaryKeyFieldsFor(Type entityType);

        /// <summary>Retrieves the required navigation properties for the given type</summary>
        IEnumerable<NavigationProperty> GetRequiredNavigationPropertiesForType(Type entityType);

        /// <summary>Retrieves the navigation properties for the given type</summary>
        IEnumerable<NavigationProperty> GetNavigationPropertiesForType(Type entityType);
    }

    internal class EntityManager : IEntityManager
    {
        private readonly DbContext _context;
        private ObjectContext ObjectContext
        {
            get { return ((IObjectContextAdapter)_context).ObjectContext; }
        }

        public EntityManager(DbContext context)
        {
            _context = context;
        }

        public EntityKey CreateEntityKey(object entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }

            return ObjectContext.CreateEntityKey(GetEntitySetName(entity.GetType()), entity);
        }

        public bool AreKeysIdentical(object newValue, object dbValue)
        {
            if (newValue == null || dbValue == null)
            {
                return false;
            }

            return CreateEntityKey(newValue) == CreateEntityKey(dbValue);
        }

        public object CreateEmptyEntityWithKey(object entity)
        {
            var instance = Activator.CreateInstance(entity.GetType(), true);
            CopyPrimaryKeyFields(entity, instance);
            return instance;
        }

        public IEnumerable<PropertyInfo> GetPrimaryKeyFieldsFor(Type entityType)
        {
            var metadata = ObjectContext.MetadataWorkspace
                    .GetItems<EntityType>(DataSpace.OSpace)
                    .SingleOrDefault(p => p.FullName == ObjectContext.GetObjectType(entityType).FullName);

            if (metadata == null)
            {
                throw new InvalidOperationException(String.Format("The type {0} is not known to the DbContext.", entityType.FullName));
            }

            return metadata.KeyMembers
                .Select(k => entityType.GetProperty(k.Name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                .ToList();
        }

        public IEnumerable<NavigationProperty> GetRequiredNavigationPropertiesForType(Type entityType)
        {
            return GetNavigationPropertiesForType(ObjectContext.GetObjectType(entityType))
                    .Where(navigationProperty => navigationProperty.ToEndMember.RelationshipMultiplicity == RelationshipMultiplicity.One);
        }

        public IEnumerable<NavigationProperty> GetNavigationPropertiesForType(Type entityType)
        {
            return ObjectContext.MetadataWorkspace
                    .GetItems<EntityType>(DataSpace.OSpace)
                    .Single(p => p.FullName == entityType.FullName)
                    .NavigationProperties;
        }

        private string GetEntitySetName(Type entityType)
        {
            Type type = entityType;
            EntitySetBase set = null;

            while (set == null && type != null)
            {
                set = ObjectContext.MetadataWorkspace
                        .GetEntityContainer(ObjectContext.DefaultContainerName, DataSpace.CSpace)
                        .EntitySets
                        .FirstOrDefault(item => item.ElementType.Name.Equals(type.Name));

                type = type.BaseType;
            }

            return set != null ? set.Name : null;
        }

        private void CopyPrimaryKeyFields(object from, object to)
        {
            var keyProperties = GetPrimaryKeyFieldsFor(from.GetType());
            foreach (var keyProperty in keyProperties)
            {
                keyProperty.SetValue(to, keyProperty.GetValue(from, null), null);
            }
        }
    }
}
