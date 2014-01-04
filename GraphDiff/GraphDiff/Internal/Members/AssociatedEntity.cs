using System.Data.Entity;
using System.Reflection;

namespace RefactorThis.GraphDiff.Internal.Members
{
    internal class AssociatedEntity : AEntityMember
    {
        internal AssociatedEntity(AMember parent, PropertyInfo accessor)
                : base(parent, accessor)
        {
        }

        protected override void UpdateInternal<T>(DbContext context, T existing, object dbValue, object newValue)
        {
            if (newValue == null)
            {
                Accessor.SetValue(existing, null, null);
                return;
            }

            // do nothing if the key is already identical
            if (context.IsKeyIdentical(newValue, dbValue))
                return;

            context.AttachAndReloadEntity(newValue);

            Accessor.SetValue(existing, newValue, null);
        }
    }
}
