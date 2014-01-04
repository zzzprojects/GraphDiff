using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Reflection;

namespace RefactorThis.GraphDiff.Internal.Members.Collections
{
    internal abstract class ACollectionMember : AMember
    {
        protected ACollectionMember(AMember parent, PropertyInfo accessor)
            : base(parent, accessor)
        {
        }

        internal override void Update<T>(DbContext context, T existing, T entity)
        {
            var updateValues = GetValue<IEnumerable>(entity);
            var dbCollection = GetValue<IEnumerable>(existing);

            if (updateValues == null)
                updateValues = new List<object>();

            Type dbCollectionType = Accessor.PropertyType;
            Type innerElementType;

            if (dbCollectionType.IsArray)
                innerElementType = dbCollectionType.GetElementType();
            else if (dbCollectionType.IsGenericType)
                innerElementType = dbCollectionType.GetGenericArguments()[0];
            else
                throw new InvalidOperationException("GraphDiff requires the collection to be either IEnumerable<T> or T[]");

            if (dbCollection == null)
            {
                var newDbCollectionType = !dbCollectionType.IsInterface ? dbCollectionType : typeof(List<>).MakeGenericType(innerElementType);
                dbCollection = (IEnumerable)Activator.CreateInstance(newDbCollectionType);
                SetValue(existing, dbCollection);
            }

            var keyFields = context.GetPrimaryKeyFieldsFor(ObjectContext.GetObjectType(innerElementType));
            var dbHash = dbCollection.Cast<object>().ToDictionary(item => DbContextExtensions.CreateHash(keyFields, item));

            // Iterate through the elements from the updated graph and try to match them against the db graph.
            var additions = new List<object>();
            foreach (var updateItem in updateValues)
            {
                var key = DbContextExtensions.CreateHash(keyFields, updateItem);

                // try to find item with same key in db collection
                object dbItem;
                if (dbHash.TryGetValue(key, out dbItem))
                {
                    // If we own the collection
                    if (this is OwnedCollection)
                    {
                        context.UpdateValuesWithConcurrencyCheck(updateItem, dbItem);

                        context.AttachCyclicNavigationProperty(existing, updateItem);

                        foreach (var childMember in Members)
                            childMember.Update(context, dbHash[key], updateItem);
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
                if (this is OwnedCollection)
                    context.Set(ObjectContext.GetObjectType(dbItem.GetType())).Remove(dbItem);

                dbCollection.GetType().GetMethod("Remove").Invoke(dbCollection, new[] { dbItem });
            }

            // Add elements marked for addition
            foreach (object newItem in additions)
            {
                if (this is AssociatedCollection)
                    context.AttachAndReloadEntity(newItem);

                // Otherwise we will add to object
                dbCollection.GetType().GetMethod("Add").Invoke(dbCollection, new[] { newItem });

                context.AttachCyclicNavigationProperty(existing, newItem);
            }
        }
    }
}
