using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Reflection;

namespace RefactorThis.GraphDiff.Internal.Members.Entities
{
    internal abstract class AEntityMember : AMember
    {
        protected AEntityMember(AMember parent, PropertyInfo accessor)
            : base(parent, accessor)
        {
        }

        internal override void Update<T>(DbContext context, T existing, T entity)
        {
            var dbValue = GetValue<object>(existing);
            var newValue = GetValue<object>(entity);
            if (dbValue == null && newValue == null)
                return;

            UpdateInternal(context, existing, dbValue, newValue);
        }

        protected abstract void UpdateInternal<T>(DbContext context, T existing, object dbValue, object newValue);

        protected static bool IsKeyIdentical(IObjectContextAdapter context, object newValue, object dbValue)
        {
            if (newValue == null || dbValue == null)
                return false;

            var keyFields = GetPrimaryKeyFieldsFor(context, ObjectContext.GetObjectType(newValue.GetType()));
            return CreateHash(keyFields, newValue) == CreateHash(keyFields, dbValue);
        }
    }
}