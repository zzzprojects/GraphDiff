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
            var dbvalue = Accessor.GetValue(existing, null);
            var newvalue = Accessor.GetValue(entity, null);
            if (dbvalue == null && newvalue == null) // No value
                return;

            // If we own the collection then we need to update the entities otherwise simple relationship update
            if (this is OwnedEntity)
            {
                // Check if the same key, if so then update values on the entity
                if (context.IsKeyIdentical(newvalue, dbvalue))
                    context.UpdateValuesWithConcurrencyCheck(newvalue, dbvalue);
                else
                    Accessor.SetValue(existing, newvalue, null);

                context.AttachCyclicNavigationProperty(existing, newvalue);

                foreach (var childMember in Members)
                    childMember.Update(context, dbvalue, newvalue);
            }
            else
            {
                if (newvalue == null)
                {
                    Accessor.SetValue(existing, null, null);
                    return;
                }

                // do nothing if the key is already identical
                if (context.IsKeyIdentical(newvalue, dbvalue))
                    return;

                context.AttachAndReloadEntity(newvalue);

                Accessor.SetValue(existing, newvalue, null);
            }
        }
    }
}