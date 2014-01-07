using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Data.Metadata.Edm;
using System.Linq;
using System.Reflection;

namespace RefactorThis.GraphDiff.Internal
{
    internal static class Extensions
    {
        public static List<PropertyInfo> GetPrimaryKeyFieldsFor(this IObjectContextAdapter context, Type entityType)
        {
            var metadata = context.ObjectContext.MetadataWorkspace
                    .GetItems<EntityType>(DataSpace.OSpace)
                    .SingleOrDefault(p => p.FullName == entityType.FullName);

            if (metadata == null)
                throw new InvalidOperationException(String.Format("The type {0} is not known to the DbContext.", entityType.FullName));

            return metadata.KeyMembers.Select(k => entityType.GetProperty(k.Name)).ToList();
        }
    }
}
