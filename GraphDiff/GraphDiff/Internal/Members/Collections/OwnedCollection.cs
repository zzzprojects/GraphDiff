using System.Collections;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Reflection;

namespace RefactorThis.GraphDiff.Internal.Members.Collections
{
    internal class OwnedCollection : ACollectionMember
    {
        internal OwnedCollection(AMember parent, PropertyInfo accessor)
                : base(parent, accessor)
        {
        }

        protected override void UpdateElement<T>(DbContext context, T existing, object updateItem, object dbItem)
        {
            base.UpdateElement(context, existing, updateItem, dbItem);

            UpdateValuesWithConcurrencyCheck(context, updateItem, dbItem);

            AttachCyclicNavigationProperty(context, existing, updateItem);

            foreach (var childMember in Members)
                childMember.Update(context, dbItem, updateItem);
        }

        protected override void RemoveElement<T>(DbContext context, object dbItem, IEnumerable dbCollection)
        {
            base.RemoveElement<T>(context, dbItem, dbCollection);

            context.Set(ObjectContext.GetObjectType(dbItem.GetType())).Remove(dbItem);
        }
    }
}