/*
 * This code is provided as is with no warranty. If you find a bug please report it on github.
 * If you would like to use the code please leave this comment at the top of the page
 * License MIT (c) Brent McKendrick 2012
 */

using System;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using RefactorThis.GraphDiff.Internal;
using RefactorThis.GraphDiff.Internal.Graph;
using RefactorThis.GraphDiff.Attributes;
using RefactorThis.GraphDiff.Internal.Caching;
using System.Collections.Generic;

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
	    public static T UpdateGraph<T>(this DbContext context, T entity, Expression<Func<IUpdateConfiguration<T>, object>> mapping = null) where T : class, new()
	    {
            GraphNode root;

            // mapping overrides attributes
            if (mapping != null)
            {
                root = new ConfigurationVisitor<T>().GetNodes(mapping);
            }
            else if (typeof(T).GetCustomAttributes(typeof(AggregateRootAttribute), true).Any())
            {
                root = new AggregateRegister(new CacheProvider()).GetEntityGraph(typeof(T));
            }
            else
            {
                root = new GraphNode();
            }
            
            var graphDiffer = new GraphDiffer<T>(root);
            return graphDiffer.Merge(context, entity);
	    }

        // TODO add IEnumerable<T> entities - requires changes to GraphDiffer to ensure one query.

        public static IQueryable<T> AggregateQuery<T>(this DbContext context) where T : class
        {
            var graph = new AggregateRegister(new CacheProvider()).GetEntityGraph(typeof(T));
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
