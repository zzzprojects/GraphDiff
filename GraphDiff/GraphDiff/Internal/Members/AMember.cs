using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
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
    }
}
