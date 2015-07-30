using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace RefactorThis.GraphDiff.Internal
{
    /// <summary>Db load queries</summary>
    internal interface IQueryLoader
    {
        T LoadEntity<T>(T entity, IEnumerable<string> includeStrings, QueryMode queryMode) where T : class;
        T LoadEntity<T>(Expression<Func<T, bool>> keyPredicate, IEnumerable<string> includeStrings, QueryMode queryMode) where T : class;
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

        public T LoadEntity<T>(T entity, IEnumerable<string> includeStrings, QueryMode queryMode) where T : class
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }

            var keyPredicate = CreateKeyPredicateExpression(entity);

            // skip loading of entities with empty integral key propeties (new entitites)
            if (keyPredicate == null)
                return null;

            return LoadEntity(keyPredicate, includeStrings, queryMode);
        }

        public T LoadEntity<T>(Expression<Func<T, bool>> keyPredicate, IEnumerable<string> includeStrings, QueryMode queryMode) where T : class
        {
            if (queryMode == QueryMode.SingleQuery)
            {
                var query = _context.Set<T>().AsQueryable();
                query = includeStrings.Aggregate(query, (current, include) => current.Include(include));
                return query.SingleOrDefault(keyPredicate);
            }

            if (queryMode == QueryMode.MultipleQuery)
            {
                // This is experimental - needs some testing.
                foreach (var include in includeStrings)
                {
                    var query = _context.Set<T>().AsQueryable();
                    query = query.Include(include);
                    query.SingleOrDefault(keyPredicate);
                }

                return _context.Set<T>().Local.AsQueryable().SingleOrDefault(keyPredicate);
            }

            throw new ArgumentOutOfRangeException("queryMode", "Unknown QueryMode");
        }

        private Expression<Func<T, bool>> CreateKeyPredicateExpression<T>(T entity)
        {
            // get key properties of T
            var keyProperties = _entityManager.GetPrimaryKeyFieldsFor(typeof(T)).ToList();
            var keyValues = keyProperties.Select(x => x.GetValue(entity, null)).ToArray();

            // prevent key predicate with empty values
            if (AllIntegralKeysEmpty(keyProperties, keyValues))
                return null;

            var parameter = Expression.Parameter(typeof(T));
            var expression = CreateEqualsExpression(keyValues[0], keyProperties[0], parameter);
            for (int i = 1; i < keyProperties.Count; i++)
            {
                expression = Expression.And(expression, CreateEqualsExpression(keyValues[i], keyProperties[i], parameter));
            }

            return Expression.Lambda<Func<T, bool>>(expression, parameter);
        }

        private static Expression CreateEqualsExpression(object keyValue, PropertyInfo keyProperty, Expression parameter)
        {
            return Expression.Equal(Expression.Property(parameter, keyProperty), Expression.Constant(keyValue, keyProperty.PropertyType));
        }

        private static bool AllIntegralKeysEmpty(IList<PropertyInfo> properties, IList<object> values)
        {
            for (var i = 0; i < properties.Count; i++)
            {
                // detect empty numeric key properties (new entity)
                if (properties[i].PropertyType == typeof(int))
                {
                    if ((int)values[i] == 0)
                        continue;
                }
                else if (properties[i].PropertyType == typeof(uint))
                {
                    if ((uint)values[i] == 0)
                        continue;
                }
                else if (properties[i].PropertyType == typeof(long))
                {
                    if ((long)values[i] == 0)
                        continue;
                }
                else if (properties[i].PropertyType == typeof(ulong))
                {
                    if ((ulong)values[i] == 0)
                        continue;
                }

                // skip this optimization for other types
                return false;
            }

            return true;
        }
    }
}