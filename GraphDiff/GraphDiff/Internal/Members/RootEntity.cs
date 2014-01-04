using System.Data.Entity;
using System.Reflection;

namespace RefactorThis.GraphDiff.Internal.Members
{
    internal class RootEntity : AMember
    {
        internal RootEntity(AMember parent, PropertyInfo accessor)
                : base(parent, accessor)
        {
        }

        internal override void Update<T>(DbContext context, T existing, T entity)
        {
            //base.Update(context, existing, entity);

            var includeStrings = EntityFrameworkIncludeHelper.GetIncludeStrings(this);

            // Get our entity with all includes needed, or add
            existing = DbContextExtensions.AddOrUpdateEntity(context, entity, includeStrings.ToArray());

            // Foreach branch perform recursive update
            foreach (var member in Members)
                member.Update(context, existing, entity);
        }
    }
}