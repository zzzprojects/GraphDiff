using RefactorThis.GraphDiff.Internal.Graph;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace RefactorThis.GraphDiff.Internal
{
    /// <summary>
    /// Db load queries
    /// </summary>
    internal interface IQueryLoader
    {
        T LoadEntity<T>(T entity, List<string> includeStrings, QueryMode queryMode) where T : class, new();
    }

    internal class QueryLoader : IQueryLoader
    {
        private readonly DbContext _context;

        public QueryLoader(DbContext context)
        {
            _context = context;
        }

        public T LoadEntity<T>(T entity, List<string> includeStrings, QueryMode queryMode) where T : class, new()
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }

            var query = _context.Set<T>().AsQueryable();

            // attach includes to IQueryable
            if (queryMode == QueryMode.SingleQuery)
            {
                query = includeStrings.Aggregate(query, (current, include) => current.Include(include));
            }
            else if (queryMode == QueryMode.MultipleQuery)
            {
                throw new NotImplementedException("run multiple queries instead of a single query");
            }
            else
            {
                throw new NotSupportedException("Unknown query mode");
            }

            // Run the find operation
            return query.SingleOrDefault(CreateKeyPredicateExpression(_context, entity));
        }

        private static Expression<Func<T, bool>> CreateKeyPredicateExpression<T>(IObjectContextAdapter context, T entity)
        {
            // get key properties of T
            var keyProperties = context.GetPrimaryKeyFieldsFor(typeof(T)).ToList();

            ParameterExpression parameter = Expression.Parameter(typeof(T));
            Expression expression = CreateEqualsExpression(entity, keyProperties[0], parameter);
            for (int i = 1; i < keyProperties.Count; i++)
            {
                expression = Expression.And(expression, CreateEqualsExpression(entity, keyProperties[i], parameter));
            }

            return Expression.Lambda<Func<T, bool>>(expression, parameter);
        }

        private static Expression CreateEqualsExpression(object entity, PropertyInfo keyProperty, Expression parameter)
        {
            return Expression.Equal(Expression.Property(parameter, keyProperty), Expression.Constant(keyProperty.GetValue(entity, null), keyProperty.PropertyType));
        }
    }
}
