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
    /// Db load queries
    /// </summary>
    internal interface IQueryLoader
    {
        T LoadEntity<T>(T entity, List<string> includeStrings, QueryMode queryMode) where T : class;
        T LoadEntity<T>(Func<T, bool> keyPredicate, List<string> includeStrings, QueryMode queryMode) where T : class;
    }

    internal class QueryLoader : IQueryLoader
    {
        private readonly DbContext _context;
        private readonly IEntityManager _entityManager;

        public QueryLoader(DbContext context, IEntityManager entityManager)
        {
            _entityManager = entityManager;
            _context = context;
        }

        public T LoadEntity<T>(T entity, List<string> includeStrings, QueryMode queryMode) where T : class
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }

            var keyPredicate = CreateKeyPredicateExpression(_context, entity);
            return LoadEntity<T>(keyPredicate, includeStrings, queryMode);
        }

        public T LoadEntity<T>(Func<T, bool> keyPredicate, List<string> includeStrings, QueryMode queryMode) where T : class
        {
            if (queryMode == QueryMode.SingleQuery)
            {
                var query = _context.Set<T>().AsQueryable();
                query = includeStrings.Aggregate(query, (current, include) => current.Include(include));
                return query.SingleOrDefault(keyPredicate);
            }
            else if (queryMode == QueryMode.MultipleQuery)
            {
                // This is experimental - needs some testing. 
                foreach (var include in includeStrings)
                {
                    var query = _context.Set<T>().AsQueryable();
                    query.Include(include);
                    query.SingleOrDefault(keyPredicate);
                }

                return _context.Set<T>().Local.SingleOrDefault(keyPredicate);
            }
            else
            {
                throw new ArgumentOutOfRangeException("Unknown QueryMode");
            }
        }

        private Func<T, bool> CreateKeyPredicateExpression<T>(IObjectContextAdapter context, T entity)
        {
            // get key properties of T
            var keyProperties = _entityManager.GetKeyFieldsFor(typeof(T)).ToList();

            ParameterExpression parameter = Expression.Parameter(typeof(T));
            Expression expression = CreateEqualsExpression(entity, keyProperties[0], parameter);
            for (int i = 1; i < keyProperties.Count; i++)
            {
                expression = Expression.And(expression, CreateEqualsExpression(entity, keyProperties[i], parameter));
            }

            return Expression.Lambda<Func<T, bool>>(expression, parameter).Compile();
        }

        private static Expression CreateEqualsExpression(object entity, PropertyInfo keyProperty, Expression parameter)
        {
            return Expression.Equal(Expression.Property(parameter, keyProperty), Expression.Constant(keyProperty.GetValue(entity, null), keyProperty.PropertyType));
        }
    }
}
