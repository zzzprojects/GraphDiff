/*
 * This code is provided as is with no warranty. If you find a bug please report it on github.
 * If you would like to use the code please leave this comment at the top of the page
 * License MIT (c) Brent McKendrick 2012
 */

using System;
using System.Data.Entity;
using System.Linq.Expressions;
using RefactorThis.GraphDiff.Internal;
using RefactorThis.GraphDiff.Internal.Members;

namespace RefactorThis.GraphDiff
{
	public static class DbContextExtensions
	{
	    /// <summary>
	    /// Attaches a graph of entities and performs an update to the data store.
	    /// Author: (c) Brent McKendrick 2012
	    /// </summary>
	    /// <typeparam name="T">The type of the root entity</typeparam>
        /// <param name="context">The database context to attach / detach.</param>
	    /// <param name="entity">The root entity.</param>
	    /// <param name="mapping">The mapping configuration to define the bounds of the graph</param>
	    public static void UpdateGraph<T>(this DbContext context, T entity, Expression<Func<IUpdateConfiguration<T>, object>> mapping = null) where T : class, new()
	    {
	        var root = mapping == null ? new RootEntity(null, null) : new ConfigurationVisitor<T>().GetMembers(mapping);
	        root.Update(context, null, entity);
	    }
	}
}
