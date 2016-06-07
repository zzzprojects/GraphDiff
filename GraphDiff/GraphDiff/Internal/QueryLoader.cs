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
        IEnumerable<T> LoadEntities<T>(IEnumerable<T> entities, IEnumerable<string> includeStrings, QueryMode queryMode) where T : class;
        IEnumerable<T> LoadEntities<T>(Expression<Func<T, bool>> keyPredicate, IEnumerable<string> includeStrings, QueryMode queryMode) where T : class;
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

        public IEnumerable<T> LoadEntities<T>(IEnumerable<T> entities, IEnumerable<string> includeStrings, QueryMode queryMode) where T : class
        {
            if (entities == null)
            {
                throw new ArgumentNullException("entities");
            }

            var keyProperties = _entityManager.GetPrimaryKeyFieldsFor(typeof(T)).ToArray();
            var keyValues = entities.Select(e => keyProperties.Select(x => x.GetValue(e, null)).ToArray()).ToArray();
            var keyPredicate = CreateKeyPredicateExpression(entities, keyProperties, keyValues);
            var entityCount = keyValues.Length;

            // skip loading of entities with empty integral key propeties (new entitites)
            if (keyPredicate == null)
                return new T[entityCount];

            // load presisted entities
            var loadedEntities = LoadEntities(keyPredicate, includeStrings, queryMode);

            // skip sort for single entities
            if (entityCount == 1)
                return new[] { loadedEntities.FirstOrDefault() };

            // restore order of loaded entities
            var orderedEntities = new List<T>(entityCount);
            foreach (var entity in entities)
            {
                var entityKeyValues = keyValues[orderedEntities.Count];
                orderedEntities.Add(loadedEntities.FirstOrDefault(x =>
                {
                    // find matching item by key values
                    for (var i = 0; i < entityKeyValues.Length; i++)
                        if (!Equals(entityKeyValues[i], keyProperties[i].GetValue(x, null)))
                            return false;
                    return true;
                }));
            }

            // validate count
            if (orderedEntities.Count != entityCount)
                throw new InvalidOperationException(
                    String.Format("Could not load all {0} persisted items of type '{1}'.",
                    entityCount, typeof(T).FullName));

            return orderedEntities;
        }

        public IEnumerable<T> LoadEntities<T>(Expression<Func<T, bool>> keyPredicate, IEnumerable<string> includeStrings, QueryMode queryMode) where T : class
        {
            if (queryMode == QueryMode.SingleQuery)
            {
                var query = _context.Set<T>().AsQueryable();
                query = includeStrings.Aggregate(query, (current, include) => current.Include(include));
                return query.Where(keyPredicate).ToArray();
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

                return _context.Set<T>().Local.AsQueryable().Where(keyPredicate).ToArray();
            }

            throw new ArgumentOutOfRangeException("queryMode", "Unknown QueryMode");
        }

        private Expression<Func<T, bool>> CreateKeyPredicateExpression<T>(IEnumerable<T> entities, IList<PropertyInfo> keyProperties, IEnumerable<IList<object>> keyValues)
        {
            // get key properties of T
            ParameterExpression parameter = Expression.Parameter(typeof(T));
            Expression resultExpression = null;
            var keyValuesEnumerator = keyValues.GetEnumerator();

            foreach (var entity in entities)
            {
                if (!keyValuesEnumerator.MoveNext())
                    throw new InvalidOperationException(
                        String.Format("Number of key values does not match number of entities with type '{0}'.",
                        typeof(T).FullName));

                // prevent key predicate with empty values
                if (GraphDiffConfiguration.SkipLoadingOfNewEntities &&
                    AllIntegralKeysEmpty(keyProperties, keyValuesEnumerator.Current))
                    continue;

                // create predicate for entity
                var itemExpression = CreateEqualsExpression(keyValuesEnumerator.Current[0], keyProperties[0], parameter);
                for (int i = 1; i < keyProperties.Count; i++)
                    itemExpression = Expression.AndAlso(itemExpression,
                        CreateEqualsExpression(keyValuesEnumerator.Current[i], keyProperties[i], parameter));

                // compose all entity predicates
                resultExpression = resultExpression != null
                    ? Expression.OrElse(resultExpression, itemExpression)
                    : itemExpression;
            }

            return resultExpression != null
                ? Expression.Lambda<Func<T, bool>>(resultExpression, parameter)
                : null;
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