/*
 * This code is provided as is with no warranty. If you find a bug please report it on github.
 * If you would like to use the code please leave this comment at the top of the page
 * License MIT (c) Brent McKendrick 2012
 */

using System;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using RefactorThis.GraphDiff.Internal;
using RefactorThis.GraphDiff.Internal.Members;
using RefactorThis.GraphDiff.Internal.Members.Entities;

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

        // attaches the navigation property of a child back to its parent (if exists)
	    internal static void AttachCyclicNavigationProperty(this IObjectContextAdapter context, object parent, object child)
        {
            if (parent == null || child == null)
                return;

            var parentType = ObjectContext.GetObjectType(parent.GetType());
            var childType = ObjectContext.GetObjectType(child.GetType());

            var navigationProperties = context.ObjectContext.MetadataWorkspace
                    .GetItems<EntityType>(DataSpace.OSpace)
                    .Single(p => p.FullName == childType.FullName)
                    .NavigationProperties;

            var parentNavigationProperty = navigationProperties
                    .Where(navigation => navigation.TypeUsage.EdmType.Name == parentType.Name)
                    .Select(navigation => childType.GetProperty(navigation.Name))
                    .FirstOrDefault();

            if (parentNavigationProperty != null)
                parentNavigationProperty.SetValue(child, parent, null);
        }

	    internal static void UpdateValuesWithConcurrencyCheck<T>(this DbContext context, T from, T to) where T : class
	    {
	        context.EnsureConcurrency(from, to);
	        context.Entry(to).CurrentValues.SetValues(from);
	    }

        // Ensures concurrency properties are checked (manual at the moment.. todo)
	    private static void EnsureConcurrency<T>(this IObjectContextAdapter db, T from, T to)
	    {
	        // get concurrency properties of T
	        var entityType = ObjectContext.GetObjectType(from.GetType());
	        var metadata = db.ObjectContext.MetadataWorkspace;

	        var objType = metadata.GetItems<EntityType>(DataSpace.OSpace).Single(p => p.FullName == entityType.FullName);

	        // need internal string, code smells bad.. any better way to do this?
	        var cTypeName = (string) objType.GetType()
	                .GetProperty("CSpaceTypeName", BindingFlags.Instance | BindingFlags.NonPublic)
	                .GetValue(objType, null);

	        var conceptualType = metadata.GetItems<EntityType>(DataSpace.CSpace).Single(p => p.FullName == cTypeName);
	        var concurrencyProperties = conceptualType.Members
                    .Where(member => member.TypeUsage.Facets.Any(facet => facet.Name == "ConcurrencyMode" && (ConcurrencyMode)facet.Value == ConcurrencyMode.Fixed))
	                .Select(member => entityType.GetProperty(member.Name))
	                .ToList();

	        // Check if concurrency properties are equal
	        // TODO EF should do this automatically should it not?
	        foreach (PropertyInfo concurrencyProp in concurrencyProperties)
	        {
	            // if is byte[] use array comparison, else equals().
                if ((concurrencyProp.PropertyType == typeof(byte[]) && !((byte[])concurrencyProp.GetValue(from, null)).SequenceEqual((byte[])concurrencyProp.GetValue(to, null)))
                    || concurrencyProp.GetValue(from, null).Equals(concurrencyProp.GetValue(to, null)))
	            {
	                throw new DbUpdateConcurrencyException(String.Format("{0} failed optimistic concurrency", concurrencyProp.Name));
	            }
	        }
	    }

	    internal static void AttachAndReloadEntity(this DbContext context, object entity)
	    {
	        if (context.Entry(entity).State == EntityState.Detached)
	            context.Set(ObjectContext.GetObjectType(entity.GetType())).Attach(entity);

	        if (GraphDiffConfiguration.ReloadAssociatedEntitiesWhenAttached)
	            context.Entry(entity).Reload();
	    }
	}
}
