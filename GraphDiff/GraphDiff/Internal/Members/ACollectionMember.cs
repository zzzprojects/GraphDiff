using System.Data.Entity;
using System.Reflection;

namespace RefactorThis.GraphDiff.Internal.Members
{
    internal abstract class ACollectionMember : AMember
    {
        protected ACollectionMember(AMember parent, PropertyInfo accessor)
            : base(parent, accessor)
        {
        }

        internal override void Update<T>(DbContext context, T existing, T entity)
        {
            DbContextExtensions.UpdateCollectionRecursive(context, existing, entity, this);
        }
    }
}
