using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Reflection;

namespace RefactorThis.GraphDiff.Internal
{
    internal static class Extensions
    {
        internal static IEnumerable<PropertyInfo> GetPrimaryKeyFieldsFor(this IObjectContextAdapter context, Type entityType)
        { 
            var metadata = context.ObjectContext.MetadataWorkspace
                    .GetEntityTypeByType(entityType);

            if (metadata == null)
            {
                throw new InvalidOperationException(String.Format("The type {0} is not known to the DbContext.", entityType.FullName));
            }

            return metadata.KeyMembers.Select(k => entityType.GetProperty(k.Name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)).ToList();
        }

        internal static IEnumerable<NavigationProperty> GetRequiredNavigationPropertiesForType(this IObjectContextAdapter context, Type entityType)
        {
            return context.GetNavigationPropertiesForType(ObjectContext.GetObjectType(entityType))
                    .Where(navigationProperty => navigationProperty.ToEndMember.RelationshipMultiplicity == RelationshipMultiplicity.One);
        }

        internal static IEnumerable<NavigationProperty> GetNavigationPropertiesForType(this IObjectContextAdapter context, Type entityType)
        {

            return context.ObjectContext.MetadataWorkspace.GetEntityTypeByType(entityType).NavigationProperties;
        }

        internal static string GetEntitySetName(this IObjectContextAdapter context, Type entityType)
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

        /// <summary>A MetadataWorkspace extension method that gets entity type by type.</summary>
        /// <param name="metadataWorkspace">The metadataWorkspace to act on.</param>
        /// <param name="entityType">Type of the entity.</param>
        /// <returns>The entity type by type.</returns>
        /// not support class in generic class
        internal static EntityType GetEntityTypeByType(this MetadataWorkspace metadataWorkspace, Type entityType)
        {
            string name = entityType.FullName.Replace("+", ".");
            var lenght = name.IndexOf("`");

            if (lenght != -1)
            {
                name = name.Substring(0, lenght);
            }

            return metadataWorkspace
                .GetItems<EntityType>(DataSpace.OSpace)
                .SingleOrDefault(p => p.FullName == name);
        }
    }
}
