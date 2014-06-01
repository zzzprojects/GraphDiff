using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Reflection;

namespace RefactorThis.GraphDiff.Internal
{
    internal static class IObjectContextAdapterExtensions
    {
        public static IEnumerable<PropertyInfo> GetPrimaryKeyFieldsFor(this IObjectContextAdapter context, Type entityType)
        {
            var metadata = context.ObjectContext.MetadataWorkspace
                    .GetItems<EntityType>(DataSpace.OSpace)
                    .SingleOrDefault(p => p.FullName == entityType.FullName);

            if (metadata == null)
            {
                throw new InvalidOperationException(String.Format("The type {0} is not known to the DbContext.", entityType.FullName));
            }

            return metadata.KeyMembers.Select(k => entityType.GetProperty(k.Name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)).ToList();
        }

        public static IEnumerable<NavigationProperty> GetRequiredNavigationPropertiesForType(this IObjectContextAdapter context, Type entityType)
        {
            return context.GetNavigationPropertiesForType(ObjectContext.GetObjectType(entityType))
                    .Where(navigationProperty => navigationProperty.ToEndMember.RelationshipMultiplicity == RelationshipMultiplicity.One);
        }

        public static IEnumerable<NavigationProperty> GetNavigationPropertiesForType(this IObjectContextAdapter context, Type entityType)
        {
            return context.ObjectContext.MetadataWorkspace
                    .GetItems<EntityType>(DataSpace.OSpace)
                    .Single(p => p.FullName == entityType.FullName)
                    .NavigationProperties;
        }

        public static string GetEntitySetName(this IObjectContextAdapter context, Type entityType)
        {
            Type type = entityType;
            EntitySetBase set = null;

            while (set == null && type != null)
            {
                set = context.ObjectContext.MetadataWorkspace
                        .GetEntityContainer(context.ObjectContext.DefaultContainerName, DataSpace.CSpace)
                        .EntitySets
                        .FirstOrDefault(item => item.ElementType.Name.Equals(type.Name));

                type = type.BaseType;
            }

            return set != null ? set.Name : null;
        }

        public static object FindEntityByKey(this DbContext context, object associatedEntity)
        {
            var associatedEntityType = ObjectContext.GetObjectType(associatedEntity.GetType());
            var keyFields = context.GetPrimaryKeyFieldsFor(associatedEntityType);
            var keys = keyFields.Select(key => key.GetValue(associatedEntity, null)).ToArray();
            return context.Set(associatedEntityType).Find(keys);
        }

        public static object CreateEmptyEntityWithKey(this IObjectContextAdapter context, object entity)
        {
            var instance = Activator.CreateInstance(entity.GetType());
            context.CopyPrimaryKeyFields(entity, instance);
            return instance;
        }

        public static void CopyPrimaryKeyFields(this IObjectContextAdapter context, object from, object to)
        {
            var keyProperties = context
                .GetPrimaryKeyFieldsFor(from.GetType())
                .ToList();

            foreach (var keyProperty in keyProperties)
            {
                keyProperty.SetValue(to, keyProperty.GetValue(from, null), null);
            }
        }
    }
}
