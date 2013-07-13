/*
 * This code is provided as is with no warranty. If you find a bug please report it on github.
 * If you would like to use the code please leave this comment at the top of the page
 * (c) Brent McKendrick 2012
 */

using RefactorThis.GraphDiff.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Objects;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace RefactorThis.GraphDiff
{
    public static class DbContextExtensions
    {
        /// <summary>
        /// Attaches a graph of entities and performs an update to the data store.
        /// Author: (c) Brent McKendrick 2012
        /// </summary>
        /// <typeparam name="T">The type of the root entity</typeparam>
        /// <param name="entity">The entity.</param>
        /// <param name="relations">The relations specified by an IUpdateConfiguration mapping</param>
        public static void UpdateGraph<T>(this DbContext context, T entity, Expression<Func<IUpdateConfiguration<T>, object>> mapping) where T : class
        {
            // Guard null mapping
            if (mapping == null)
            {
                // Redirect to simple update
                UpdateGraph<T>(context, entity);
                return;
            }

            bool isAutoDetectEnabled = context.Configuration.AutoDetectChangesEnabled;
            try
            {
                // performance improvement for large graphs
                context.Configuration.AutoDetectChangesEnabled = false;

                // Parse mapping tree
                var tree = new UpdateConfigurationVisitor<T>().GetUpdateMembers(mapping);
                var includeExpressions = EFIncludeHelper.GetIncludeExpressions<T>(tree);

                // Get our entity with all includes needed
                T existing = context.FindEntityMatching(entity, includeExpressions.ToArray());

                // Force update of parent entity
                context.Entry(existing).CurrentValues.SetValues(entity);

                // Foreach branch perform recursive update
                foreach (var member in tree.Members)
                    RecursiveGraphUpdate(context, existing, entity, member);
            }
            finally
            {
                context.Configuration.AutoDetectChangesEnabled = isAutoDetectEnabled;
            }
        }
        public static void UpdateGraph<T>(this DbContext context, T entity) where T : class
        {
            // Get our entity and force update
            T existing = context.FindEntityMatching(entity);
            context.Entry(existing).CurrentValues.SetValues(entity);
        }

        #region Private

        /// <summary>
        /// Updates a detached graph of entities by performing a diff comparison of object keys.
        /// Author: Brent McKendrick
        /// </summary>
        /// <param name="context">The database context to attach / detach.</param>
        /// <param name="dataStoreEntity">The entity (sub)graph as retrieved from the data store.</param>
        /// <param name="updatingEntity">The entity (sub)graph after it has been updated</param>
        private static void RecursiveGraphUpdate(DbContext context, object dataStoreEntity, object updatingEntity, UpdateMember member)
        {
            if (member.IsCollection)
            {
                // We are dealing with a collection
                var updateValues = (IEnumerable)member.Accessor.GetValue(updatingEntity, null);
                var dbCollection = (IEnumerable)member.Accessor.GetValue(dataStoreEntity, null);
                var keyFields = context.GetKeysFor(updateValues.GetType().GetGenericArguments()[0]);
                var dbHash = MapCollectionToDictionary(keyFields, dbCollection);

                // Iterate through the elements from the updated graph and try to match them against the db graph.
                List<object> additions = new List<object>();
                foreach (object updateItem in updateValues)
                {
                    var key = CreateHash(keyFields, updateItem);
                    // try to find in db collection
                    object dbItem;
                    if (dbHash.TryGetValue(key, out dbItem))
                    {
                        // If we own the collection
                        if (member.IsOwned)
                        {
                            context.Entry(dbItem).CurrentValues.SetValues(updateItem); // match means we are updating
                            AttachCyclicNavigationProperty(context, dataStoreEntity, updateItem);

                            foreach (var childMember in member.Members)
                                RecursiveGraphUpdate(context, dbHash[key], updateItem, childMember);
                        }

                        dbHash.Remove(key); // remove to leave only db removals in the collection
                    }
                    else
                        additions.Add(updateItem);
                }

                // Removal of dbItem's left in the collection
                foreach (var dbItem in dbHash.Values)
                {
                    // Own the collection so remove it completely.
                    if (member.IsOwned)
                        context.Set(ObjectContext.GetObjectType(dbItem.GetType())).Remove(dbItem);

                    dbCollection.GetType().GetMethod("Remove").Invoke(dbCollection, new[] { dbItem });
                }

                // Add elements marked for addition
                foreach (object newItem in additions)
                {
                    if (!member.IsOwned)
                        context.Set(ObjectContext.GetObjectType(newItem.GetType())).Attach(newItem);

                    // Otherwise we will add to object
                    dbCollection.GetType().GetMethod("Add").Invoke(dbCollection, new[] { newItem });
                    AttachCyclicNavigationProperty(context, dataStoreEntity, newItem);
                }
            }
            else // not collection
            {
                var dbvalue = member.Accessor.GetValue(dataStoreEntity, null);
                var newvalue = member.Accessor.GetValue(updatingEntity, null);
                if (dbvalue == null && newvalue == null) // No value
                    return;

                // If we own the collection then we need to update the entities otherwise simple relationship update
                if (!member.IsOwned)
                {
                    if (newvalue == null)
                    {
                        member.Accessor.SetValue(dataStoreEntity, null, null);
                        return;
                    }

                    if (dbvalue != null && newvalue != null)
                    {
                        var keyFields = context.GetKeysFor(ObjectContext.GetObjectType(newvalue.GetType()));
                        var newKey = CreateHash(keyFields, newvalue);
                        var updateKey = CreateHash(keyFields, dbvalue);
                        if (newKey == updateKey)
                            return; // do nothing if the same key
                    }

                    if (context.Entry(newvalue).State == EntityState.Detached)
                        context.Set(ObjectContext.GetObjectType(newvalue.GetType())).Attach(newvalue);

                    member.Accessor.SetValue(dataStoreEntity, newvalue, null);
                    context.Entry(newvalue).Reload();
                    // TODO would like to do this: context.Entry(newvalue).State = EntityState.Unchanged;
                    // However it seems even though we are in an unchanged state EF will still update the database if the original values are different.
                }
                else
                {
                    if (dbvalue != null && newvalue != null)
                    {
                        // Check if the same key, if so then update values on the entity
                        var keyFields = context.GetKeysFor(ObjectContext.GetObjectType(newvalue.GetType()));
                        var newKey = CreateHash(keyFields, newvalue);
                        var updateKey = CreateHash(keyFields, dbvalue);

                        // perform update if the same
                        if (updateKey == newKey)
                        {
                            context.Entry(dbvalue).CurrentValues.SetValues(newvalue);
                        }
                        else
                            member.Accessor.SetValue(dataStoreEntity, newvalue, null);
                    }
                    else
                        member.Accessor.SetValue(dataStoreEntity, newvalue, null);

                    AttachCyclicNavigationProperty(context, dataStoreEntity, newvalue);

                    // TODO
                    foreach (var childMember in member.Members)
                        RecursiveGraphUpdate(context, dbvalue, newvalue, childMember);
                }
            }
        }
        private static Dictionary<string, object> MapCollectionToDictionary(IEnumerable<PropertyInfo> keyfields, IEnumerable enumerable)
        {
            var hash = new Dictionary<string, object>();
            foreach (object item in enumerable)
                hash.Add(CreateHash(keyfields, item), item);
            return hash;
        }
        private static string CreateHash(IEnumerable<PropertyInfo> keys, object entity)
        {
            // Create unique string representing the keys
            string code = "";

            foreach (var property in keys)
                code += "|" + property.GetValue(entity, null).GetHashCode();

            return code;
        }


        // attaches the navigation property of a child back to its parent (if exists)
        public static void AttachCyclicNavigationProperty(DbContext db, object parent, object child)
        {
            if (parent == null || child == null)
                return;

            var parentType = ObjectContext.GetObjectType(parent.GetType());
            var childType = ObjectContext.GetObjectType(child.GetType());
            var objectContext = ((System.Data.Entity.Infrastructure.IObjectContextAdapter)db).ObjectContext;
            MethodInfo m = objectContext.GetType().GetMethod("CreateObjectSet", new Type[] { });
            MethodInfo generic = m.MakeGenericMethod(childType);
            object set = generic.Invoke(objectContext, null);

            PropertyInfo entitySetPI = set.GetType().GetProperty("EntitySet");
            System.Data.Metadata.Edm.EntitySet entitySet = (System.Data.Metadata.Edm.EntitySet)entitySetPI.GetValue(set, null);

            foreach (var prop in entitySet.ElementType.NavigationProperties)
            {
                if (prop.TypeUsage.EdmType.Name == parentType.Name)
                {
                    var propInfo = childType.GetProperty(prop.Name);
                    propInfo.SetValue(child, parent, null);
                    break;
                }
            }
        }

        #endregion

        #region Extensions

        public static T FindEntityMatching<T>(this DbContext context, T entity, params Expression<Func<T, object>>[] includes) where T : class
        {
            var query = context.Set<T>().AsQueryable();
            foreach (var include in includes)
                query = query.Include(include);

            var keyProperties = context.GetKeysFor(typeof(T)).ToList();

            var values = new List<object>();
            foreach (PropertyInfo keyProp in keyProperties)
                values.Add(keyProp.GetValue(entity, null));

            // Run the find operation
            ParameterExpression parameter = Expression.Parameter(typeof(T));
            Expression expression = Expression.Equal(Expression.Property(parameter, keyProperties[0]), Expression.Constant(values[0]));
            for (int i = 1; i < keyProperties.Count; i++)
            {
                expression = Expression.And(expression,
                    Expression.Equal(Expression.Property(parameter, keyProperties[i]), Expression.Constant(values[i])));
            }
            var lambda = Expression.Lambda<Func<T, bool>>(expression, parameter);
            return query.Single<T>(lambda);

        }

        // Gets the primary key fields for an entity type.
        public static IEnumerable<PropertyInfo> GetKeysFor(this DbContext db, Type entityType)
        {
            var objectContext = ((System.Data.Entity.Infrastructure.IObjectContextAdapter)db).ObjectContext;
            MethodInfo m = objectContext.GetType().GetMethod("CreateObjectSet", new Type[] { });
            MethodInfo generic = m.MakeGenericMethod(entityType);
            object set = generic.Invoke(objectContext, null);

            PropertyInfo entitySetPI = set.GetType().GetProperty("EntitySet");
            System.Data.Metadata.Edm.EntitySet entitySet = (System.Data.Metadata.Edm.EntitySet)entitySetPI.GetValue(set, null);

            foreach (string name in entitySet.ElementType.KeyMembers.Select(k => k.Name))
                yield return entityType.GetProperty(name);
        }

        #endregion

    }
}
