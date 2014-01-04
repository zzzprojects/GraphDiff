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
            var innerElementType = GetCollectionElementType();
            var updateValues = GetValue<IEnumerable>(entity) ?? new List<object>();
            var dbCollection = GetValue<IEnumerable>(existing) ?? CreateMissingCollection(existing, innerElementType);

            var keyFields = GetPrimaryKeyFieldsFor(context, ObjectContext.GetObjectType(innerElementType));
            var dbHash = dbCollection.Cast<object>().ToDictionary(item => CreateHash(keyFields, item));

            // Iterate through the elements from the updated graph and try to match them against the db graph
            foreach (var updateItem in updateValues)
            {
                var key = CreateHash(keyFields, updateItem);

                // try to find item with same key in db collection
                object dbItem;
                if (dbHash.TryGetValue(key, out dbItem))
                {
                    if (this is OwnedCollection)
                    {
                        UpdateValuesWithConcurrencyCheck(context, updateItem, dbItem);

                        AttachCyclicNavigationProperty(context, existing, updateItem);

                        foreach (var childMember in Members)
                            childMember.Update(context, dbHash[key], updateItem);
                    }
                    dbHash.Remove(key); // remove to leave only db removals in the collection
                }
                else
                    AddElement(context, existing, updateItem, dbCollection);
            }

            // remove obsolete items
            foreach (var dbItem in dbHash.Values)
                RemoveElement<T>(context, dbItem, dbCollection);
        }

        protected virtual void AddElement<T>(DbContext context, T existing, object updateItem, IEnumerable dbCollection)
        {
            dbCollection.GetType().GetMethod("Add").Invoke(dbCollection, new[] {updateItem});

            AttachCyclicNavigationProperty(context, existing, updateItem);
        }

        //protected virtual void UpdateElement<T>(DbContext context, T existing, object updateItem, object dbItem)
        //{
        //}

        protected virtual void RemoveElement<T>(DbContext context, object dbItem, IEnumerable dbCollection)
        {
            dbCollection.GetType().GetMethod("Remove").Invoke(dbCollection, new[] { dbItem });
        }

        private IEnumerable CreateMissingCollection(object existing, Type elementType)
        {
            var collectionType = !Accessor.PropertyType.IsInterface ? Accessor.PropertyType : typeof(List<>).MakeGenericType(elementType);
            var collection = (IEnumerable)Activator.CreateInstance(collectionType);
            SetValue(existing, collection);
            return collection;
        }

        private Type GetCollectionElementType()
        {
            if (Accessor.PropertyType.IsArray)
                return Accessor.PropertyType.GetElementType();

            if (Accessor.PropertyType.IsGenericType)
                return Accessor.PropertyType.GetGenericArguments()[0];

            throw new InvalidOperationException("GraphDiff requires the collection to be either IEnumerable<T> or T[]");
        }
    }
}
