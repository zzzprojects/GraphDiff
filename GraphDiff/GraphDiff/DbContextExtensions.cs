/*
 * This code is provided as is with no warranty. If you find a bug please report it on github.
 * If you would like to use the code please leave this comment at the top of the page
 * License MIT (c) Brent McKendrick 2012
 */

using RefactorThis.GraphDiff.Internal;
using RefactorThis.GraphDiff.Internal.Caching;
using RefactorThis.GraphDiff.Internal.Graph;
using RefactorThis.GraphDiff.Internal.GraphBuilders;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
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
        /// <returns>The attached entity graph</returns>
	    public static T UpdateGraph<T>(this DbContext context, T entity, Expression<Func<IUpdateConfiguration<T>, object>> mapping) where T : class, new()
	    {
            return UpdateGraph<T>(context, entity, new UpdateParams<T>
            {
                QueryMode = QueryMode.SingleQuery,
                Mapping = mapping
            });
	    }

        public static T UpdateGraph<T>(this DbContext context, T entity, UpdateParams<T> updateParams = null) where T : class, new()
        {
            GraphNode root;
            GraphDiffer<T> differ;
            var register = new AggregateRegister(new CacheProvider());

            if (updateParams == null)
            {
                differ = new GraphDiffer<T>(context, register.GetEntityGraph<T>());
                return differ.Merge(entity);
            }

            if (updateParams.Mapping != null)
            {
                // mapping configuration
                root = register.GetEntityGraph<T>(updateParams.Mapping);
            }
            else if (updateParams.MappingScheme != null)
            {
                // names scheme
                root = register.GetEntityGraph<T>(updateParams.MappingScheme);
            }
            else
            {
                // attributes or null
                root = register.GetEntityGraph<T>();
            }

            differ = new GraphDiffer<T>(context, root);
            return differ.Merge(entity, updateParams.QueryMode);
        }

        public static IQueryable<T> AggregateQuery<T>(this DbContext context, QueryMode queryMode = QueryMode.SingleQuery) where T : class
        {
            if (queryMode == QueryMode.MultipleQuery)
            {
                throw new NotImplementedException();
            }

            var graph = new AggregateRegister(new CacheProvider()).GetEntityGraph<T>();
            if (graph == null)
            {
                throw new NotSupportedException("Type: '" + typeof(T).FullName + "' is not a known aggregate");
            }

            var query = context.Set<T>().AsQueryable();

            // attach includes to IQueryable
            var includeStrings = new List<string>();
            graph.GetIncludeStrings(context, includeStrings);
            query = includeStrings.Aggregate(query, (current, include) => current.Include(include));

            return query;
        }
	}
}
