/*
 * This code is provided as is with no warranty. If you find a bug please report it on github.
 * If you would like to use the code please leave this comment at the top of the page
 * License MIT (c) Brent McKendrick 2012
 */

using RefactorThis.GraphDiff.Internal;
using RefactorThis.GraphDiff.Internal.Caching;
using RefactorThis.GraphDiff.Internal.Graph;
using System;
using System.Data.Entity;
using System.Linq.Expressions;

namespace RefactorThis.GraphDiff
{
	public static class DbContextExtensions
	{
        /// <summary>
        /// Merges a graph of entities with the data store.
        /// </summary>
        /// <typeparam name="T">The type of the root entity</typeparam>
        /// <param name="context">The database context to attach / detach.</param>
        /// <param name="entity">The root entity.</param>
        /// <param name="mapping">The mapping configuration to define the bounds of the graph</param>
        /// <param name="updateParams">Update configuration overrides</param>
        /// <param name="keysConfiguration">The mapping configuration to define properties to use as key. The primary key is used if no other configuration is given.</param>
        /// <returns>The attached entity graph</returns>
        public static T UpdateGraph<T>(this DbContext context, T entity, Expression<Func<IUpdateConfiguration<T>, object>> mapping, UpdateParams updateParams = null, KeysConfiguration keysConfiguration = null) where T : class, new()
	    {
            return UpdateGraph<T>(context, entity, mapping, null, updateParams, keysConfiguration);
	    }

        /// <summary>
        /// Merges a graph of entities with the data store.
        /// </summary>
        /// <typeparam name="T">The type of the root entity</typeparam>
        /// <param name="context">The database context to attach / detach.</param>
        /// <param name="entity">The root entity.</param>
        /// <param name="mappingScheme">Pre-configured mappingScheme</param>
        /// <param name="updateParams">Update configuration overrides</param>
        /// <param name="keysConfiguration">The mapping configuration to define properties to use as key. The primary key is used if no other configuration is given.</param>
        /// <returns>The attached entity graph</returns>
        public static T UpdateGraph<T>(this DbContext context, T entity, string mappingScheme, UpdateParams updateParams = null, KeysConfiguration keysConfiguration = null) where T : class, new()
        {
            return UpdateGraph<T>(context, entity, null, mappingScheme, updateParams, keysConfiguration);
        }

        /// <summary>
        /// Merges a graph of entities with the data store.
        /// </summary>
        /// <typeparam name="T">The type of the root entity</typeparam>
        /// <param name="context">The database context to attach / detach.</param>
        /// <param name="entity">The root entity.</param>
        /// <param name="updateParams">Update configuration overrides</param>
        /// <param name="keysConfiguration">The mapping configuration to define properties to use as key. The primary key is used if no other configuration is given.</param>
        /// <returns>The attached entity graph</returns>
        public static T UpdateGraph<T>(this DbContext context, T entity, UpdateParams updateParams = null, KeysConfiguration keysConfiguration = null) where T : class, new()
        {
            return UpdateGraph<T>(context, entity, null, null, updateParams, keysConfiguration);
        }

        /// <summary>
        /// Load an aggregate type from the database (including all related entities)
        /// </summary>
        /// <typeparam name="T">Type of the entity</typeparam>
        /// <param name="context">DbContext</param>
        /// <param name="keyPredicate">The predicate used to find the aggregate by key</param>
        /// <param name="queryMode">Load all objects at once, or perform multiple queries</param>
        /// <returns>The aggregate loaded from the database</returns>
        public static T LoadAggregate<T>(this DbContext context, Func<T, bool> keyPredicate, QueryMode queryMode = QueryMode.SingleQuery) where T : class
        {
            var entityManager = new EntityManager(context, new KeysConfiguration());
            var graph = new AggregateRegister(new CacheProvider()).GetEntityGraph<T>();
            var queryLoader = new QueryLoader(context, entityManager);

            if (graph == null)
            {
                throw new NotSupportedException("Type: '" + typeof(T).FullName + "' is not a known aggregate");
            }

            var includeStrings = graph.GetIncludeStrings(entityManager);
            return queryLoader.LoadEntity(keyPredicate, includeStrings, queryMode);
        }


        // other methods are convenience wrappers around this.
        private static T UpdateGraph<T>(this DbContext context, T entity, Expression<Func<IUpdateConfiguration<T>, object>> mapping,
                                                        string mappingScheme, UpdateParams updateParams, KeysConfiguration keysConfiguration) where T : class, new()
        {
            GraphNode root;
            GraphDiffer<T> differ;

            var entityManager = new EntityManager(context, keysConfiguration ?? new KeysConfiguration());
            var queryLoader = new QueryLoader(context, entityManager);
            var register = new AggregateRegister(new CacheProvider());

            if (updateParams == null)
            {
                updateParams = new UpdateParams { QueryMode = QueryMode.SingleQuery };
            }

            if (mapping != null)
            {
                // mapping configuration
                root = register.GetEntityGraph<T>(mapping);
            }
            else if (mappingScheme != null)
            {
                // names scheme
                root = register.GetEntityGraph<T>(mappingScheme);
            }
            else
            {
                // attributes or null
                root = register.GetEntityGraph<T>();
            }

            differ = new GraphDiffer<T>(context, queryLoader, entityManager, root);
            return differ.Merge(entity, updateParams.QueryMode);
        }

	}
}
