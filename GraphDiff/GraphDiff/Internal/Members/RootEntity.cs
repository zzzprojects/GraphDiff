using System;
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
            bool isAutoDetectEnabled = context.Configuration.AutoDetectChangesEnabled;
            try
            {
                // performance improvement for large graphs
                context.Configuration.AutoDetectChangesEnabled = false;

                var includeStrings = EntityFrameworkIncludeHelper.GetIncludeStrings(this);

                // Get our entity with all includes needed, or add
                existing = AddOrUpdateEntity(context, entity, includeStrings.ToArray());

                // Foreach branch perform recursive update
                foreach (AMember member in Members)
                    member.Update(context, existing, entity);
            }
            finally
            {
                context.Configuration.AutoDetectChangesEnabled = isAutoDetectEnabled;
            }
        }

        private static T AddOrUpdateEntity<T>(DbContext context, T entity, params string[] includes) where T : class, new()
        {
            if (entity == null)
                throw new ArgumentNullException("entity");

            T existing = context.FindEntityMatching(entity, includes);
            if (existing == null)
            {
                existing = new T();
                context.Set<T>().Add(existing);
            }
            context.UpdateValuesWithConcurrencyCheck(entity, existing);

            return existing;
        }
    }
}