using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

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
