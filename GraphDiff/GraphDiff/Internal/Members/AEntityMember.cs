using System.Data.Entity;
using System.Reflection;

namespace RefactorThis.GraphDiff.Internal.Members
{
    internal abstract class AEntityMember : AMember
    {
        protected AEntityMember(AMember parent, PropertyInfo accessor)
            : base(parent, accessor)
        {
        }

        internal override void Update<T>(DbContext context, T existing, T entity)
        {
            var dbValue = Accessor.GetValue(existing, null);
            var newValue = Accessor.GetValue(entity, null);
            if (dbValue == null && newValue == null)
                return;

            UpdateInternal(context, existing, dbValue, newValue);
        }

        protected abstract void UpdateInternal<T>(DbContext context, T existing, object dbValue, object newValue);
    }
}