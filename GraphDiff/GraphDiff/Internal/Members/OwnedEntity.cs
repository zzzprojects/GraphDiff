using System.Data.Entity;
using System.Reflection;

namespace RefactorThis.GraphDiff.Internal.Members
{
    internal class OwnedEntity : AEntityMember
    {
        internal OwnedEntity(AMember parent, PropertyInfo accessor)
                : base(parent, accessor)
        {
        }

        protected override void UpdateInternal<T>(DbContext context, T existing, object dbValue, object newValue)
        {
            // Check if the same key, if so then update values on the entity
            if (context.IsKeyIdentical(newValue, dbValue))
                context.UpdateValuesWithConcurrencyCheck(newValue, dbValue);
            else
                Accessor.SetValue(existing, newValue, null);

            context.AttachCyclicNavigationProperty(existing, newValue);

            foreach (var childMember in Members)
                childMember.Update(context, dbValue, newValue);
        }
    }
}
