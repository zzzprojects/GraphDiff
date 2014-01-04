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
            DbContextExtensions.UpdateEntityRecursive(context, existing, entity, this);
        }
    }
}