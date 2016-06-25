/*
 * This code is provided as is with no warranty. If you find a bug please report it on github.
 * If you would like to use the code please leave this comment at the top of the page
 * License MIT (c) Brent McKendrick 2012
 */

using System;
using System.Data.Entity;
using System.Linq.Expressions;
using RefactorThis.GraphDiff.Internal;
using RefactorThis.GraphDiff.Internal.Caching;
using RefactorThis.GraphDiff.Internal.Graph;

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
        /// /// <param name="persisted">The persisted entity, optional, for performance optimisation.</param>
        /// <param name="mapping">The mapping configuration to define the bounds of the graph</param>
        /// <returns>The attached entity graph</returns>
        public static T UpdateGraph<T>(this DbContext context, T entity, T persisted, Expression<Func<IUpdateConfiguration<T>, object>> mapping) where T : class
        {
            return UpdateGraph(context, entity, persisted, mapping, null, null);
        }

        /// <summary>
        /// Merges a graph of entities with the data store.
        /// </summary>
        /// <typeparam name="T">The type of the root entity</typeparam>
        /// <param name="context">The database context to attach / detach.</param>
        /// <param name="entity">The root entity.</param>
        /// <param name="mapping">The mapping configuration to define the bounds of the graph</param>
        /// <returns>The attached entity graph</returns>
        public static T UpdateGraph<T>(this DbContext context, T entity, Expression<Func<IUpdateConfiguration<T>, object>> mapping) where T : class
        {
            return UpdateGraph(context, entity, null, mapping, null, null);
        }

        /// <summary>
        /// Merges a graph of entities with the data store.
        /// </summary>
        /// <typeparam name="T">The type of the root entity</typeparam>
        /// <param name="context">The database context to attach / detach.</param>
        /// <param name="entity">The root entity.</param>
        /// <param name="mapping">The mapping configuration to define the bounds of the graph</param>
        /// <param name="updateParams">Update configuration overrides</param>
        /// <returns>The attached entity graph</returns>
        public static T UpdateGraph<T>(this DbContext context, T entity, Expression<Func<IUpdateConfiguration<T>, object>> mapping, UpdateParams updateParams = null) where T : class
        {
            return UpdateGraph(context, entity, null, mapping, null, updateParams);
        }

        /// <summary>
        /// Merges a graph of entities with the data store.
        /// </summary>
        /// <typeparam name="T">The type of the root entity</typeparam>
        /// <param name="context">The database context to attach / detach.</param>
        /// <param name="entity">The root entity.</param>
        /// <param name="mappingScheme">Pre-configured mappingScheme</param>
        /// <param name="updateParams">Update configuration overrides</param>
        /// <returns>The attached entity graph</returns>
        public static T UpdateGraph<T>(this DbContext context, T entity, string mappingScheme, UpdateParams updateParams = null) where T : class
        {
            return UpdateGraph(context, entity, null, null, mappingScheme, updateParams);
        }

        /// <summary>
        /// Merges a graph of entities with the data store.
        /// </summary>
        /// <typeparam name="T">The type of the root entity</typeparam>
        /// <param name="context">The database context to attach / detach.</param>
        /// <param name="entity">The root entity.</param>
        /// <param name="updateParams">Update configuration overrides</param>
        /// <returns>The attached entity graph</returns>
        public static T UpdateGraph<T>(this DbContext context, T entity, UpdateParams updateParams = null) where T : class
        {
            return UpdateGraph(context, entity, null, null, null, updateParams);
        }

        /// <summary>
        /// Load an aggregate type from the database (including all related entities)
        /// </summary>
        /// <typeparam name="T">Type of the entity</typeparam>
        /// <param name="context">DbContext</param>
        /// <param name="keyPredicate">The predicate used to find the aggregate by key</param>
        /// <param name="queryMode">Load all objects at once, or perform multiple queries</param>
        /// <returns>The aggregate loaded from the database</returns>
        public static T LoadAggregate<T>(this DbContext context, Expression<Func<T, bool>> keyPredicate, QueryMode queryMode = QueryMode.SingleQuery) where T : class
        {
            var entityManager = new EntityManager(context);
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
        private static T UpdateGraph<T>(this DbContext context, T entity, T persisted, Expression<Func<IUpdateConfiguration<T>, object>> mapping,
                                        string mappingScheme, UpdateParams updateParams) where T : class
        {
            if (entity == null)
                throw new ArgumentNullException("entity");

            var entityManager = new EntityManager(context);
            var queryLoader = new QueryLoader(context, entityManager);
            var register = new AggregateRegister(new CacheProvider());

            var root = GetRootNode(mapping, mappingScheme, register);
            var differ = new GraphDiffer<T>(context, queryLoader, entityManager, root);

            var queryMode = updateParams != null ? updateParams.QueryMode : QueryMode.SingleQuery;
            return differ.Merge(entity, persisted, queryMode);
        }

        private static GraphNode GetRootNode<T>(Expression<Func<IUpdateConfiguration<T>, object>> mapping, string mappingScheme, AggregateRegister register) where T : class
        {
            GraphNode root;
            if (mapping != null)
            {
                // mapping configuration
                root = register.GetEntityGraph(mapping);
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
            return root;
        }
    }
}
