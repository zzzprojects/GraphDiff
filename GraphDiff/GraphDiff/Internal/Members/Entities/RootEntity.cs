using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace RefactorThis.GraphDiff.Internal.Members.Entities
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

                // Get our entity with all includes needed, or add
                existing = AddOrUpdateEntity(context, entity);

                // Foreach branch perform recursive update
                foreach (AMember member in Members)
                    member.Update(context, existing, entity);
            }
            finally
            {
                context.Configuration.AutoDetectChangesEnabled = isAutoDetectEnabled;
            }
        }

        private T AddOrUpdateEntity<T>(DbContext context, T entity) where T : class, new()
        {
            if (entity == null)
                throw new ArgumentNullException("entity");

            T existing = FindEntityMatching(context, entity);
            if (existing == null)
            {
                existing = new T();
                context.Set<T>().Add(existing);
            }
            context.UpdateValuesWithConcurrencyCheck(entity, existing);

            return existing;
        }

        private T FindEntityMatching<T>(DbContext context, T entity) where T : class
        {
            // attach includes to IQueryable
            var query = context.Set<T>().AsQueryable();
            foreach (var include in GetIncludeStrings(this))
                query = query.Include(include);

            // Run the find operation
            return query.SingleOrDefault(CreateKeyPredicateExpression(context, entity));
        }

        private static IEnumerable<string> GetIncludeStrings(AMember root)
        {
            var list = new List<string>();
            if (root.Members.Count == 0 && root.IncludeString != null)
            {
                list.Add(root.IncludeString);
            }
            foreach (var member in root.Members)
            {
                list.AddRange(GetIncludeStrings(member));
            }
            return list;
        }

        private static Expression<Func<T, bool>> CreateKeyPredicateExpression<T>(IObjectContextAdapter context, T entity) where T : class
        {
            // get key properties of T
            var keyProperties = context.GetPrimaryKeyFieldsFor(typeof(T)).ToList();

            ParameterExpression parameter = Expression.Parameter(typeof(T));
            Expression expression = CreateEqualsExpression(entity, keyProperties[0], parameter);
            for (int i = 1; i < keyProperties.Count; i++)
                expression = Expression.And(expression, CreateEqualsExpression(entity, keyProperties[i], parameter));
            return Expression.Lambda<Func<T, bool>>(expression, parameter);
        }

        private static Expression CreateEqualsExpression(object entity, PropertyInfo keyProperty, Expression parameter)
        {
            return Expression.Equal(Expression.Property(parameter, keyProperty), Expression.Constant(keyProperty.GetValue(entity, null)));
        }
    }
}