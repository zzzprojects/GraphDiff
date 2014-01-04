using System.Collections;
using System.Data.Entity;
using System.Reflection;

namespace RefactorThis.GraphDiff.Internal.Members.Collections
{
    internal class AssociatedCollection : ACollectionMember
    {
        internal AssociatedCollection(AMember parent, PropertyInfo accessor)
                : base(parent, accessor)
        {
        }

        protected override void AddElement<T>(DbContext context, T existing, object updateItem, IEnumerable dbCollection)
        {
            AttachAndReloadEntity(context, updateItem);

            base.AddElement(context, existing, updateItem, dbCollection);
        }
    }
}