using RefactorThis.GraphDiff.Internal.Graph;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace RefactorThis.GraphDiff.Internal
{
    /// <summary>
    /// GraphDiff main access point.
    /// </summary>
    /// <typeparam name="T">The root agreggate type</typeparam>
    internal class GraphDiffer<T> where T : class, new()
    {
        private readonly GraphNode _root;

        public GraphDiffer(GraphNode root)
        {
            _root = root;
        }

        public T Merge(DbContext context, T updating)
        {
            bool isAutoDetectEnabled = context.Configuration.AutoDetectChangesEnabled;
            try
            {
                // performance improvement for large graphs
                context.Configuration.AutoDetectChangesEnabled = false;

                // Get our entity with all includes needed, or add
                T persisted = GetOrAddPersistedEntity(context, updating);

                if (context.Entry(updating).State != EntityState.Detached)
                    throw new InvalidOperationException("GraphDiff supports detached entities only at this time. Please try AsNoTracking() or detach your entites before calling the UpdateGraph method");

                // Perform recursive update
                _root.Update(context, persisted, updating);

                return persisted;
            }
            finally
            {
                context.Configuration.AutoDetectChangesEnabled = isAutoDetectEnabled;
            }
        }

        private T GetOrAddPersistedEntity(DbContext context, T entity)
        {
            if (entity == null)
                throw new ArgumentNullException("entity");

            var persisted = FindEntityMatching(context, entity);

            if (persisted == null)
            {
                // we are always working with 2 graphs, simply add a 'persisted' one if none exists,
                // this ensures that only the changes we make within the bounds of the mapping are attempted.
                persisted = new T();
                context.Set<T>().Add(persisted);
            }

            return persisted;
        }

        private T FindEntityMatching(DbContext context, T entity)
        {
            var includeStrings = new List<string>();
            _root.GetIncludeStrings(context, includeStrings);

            // attach includes to IQueryable
            var query = context.Set<T>().AsQueryable();
            query = includeStrings.Aggregate(query, (current, include) => current.Include(include));

            // Run the find operation
            return query.SingleOrDefault(CreateKeyPredicateExpression(context, entity));
        }

        private static Expression<Func<T, bool>> CreateKeyPredicateExpression(IObjectContextAdapter context, T entity)
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
            return Expression.Equal(Expression.Property(parameter, keyProperty), Expression.Constant(keyProperty.GetValue(entity, null), keyProperty.PropertyType));
        }
    }
}