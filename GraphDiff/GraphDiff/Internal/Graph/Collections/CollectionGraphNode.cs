using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Reflection;

namespace RefactorThis.GraphDiff.Internal.Graph.Collections
{
    internal class CollectionGraphNode : GraphNode
    {
        private readonly bool _isOwned;

        internal CollectionGraphNode(GraphNode parent, PropertyInfo accessor, bool isOwned)
            : base(parent, accessor)
        {
            _isOwned = isOwned;
        }

        public override void Update<T>(DbContext context, T existing, T entity)
        {
            var innerElementType = GetCollectionElementType();
            var updateValues = GetValue<IEnumerable>(entity) ?? new List<object>();
            var dbCollection = GetValue<IEnumerable>(existing) ?? CreateMissingCollection(existing, innerElementType);

            var keyFields = context.GetPrimaryKeyFieldsFor(ObjectContext.GetObjectType(innerElementType));
            var dbHash = dbCollection.Cast<object>().ToDictionary(item => CreateHashKey(keyFields, item));

            // Iterate through the elements from the updated graph and try to match them against the db graph
            foreach (var updateItem in updateValues)
            {
                var key = CreateHashKey(keyFields, updateItem);

                // try to find item with same key in db collection
                object dbItem;
                if (dbHash.TryGetValue(key, out dbItem))
                {
                    UpdateElement(context, existing, updateItem, dbItem);
                    dbHash.Remove(key);
                }
                else
                    AddElement(context, existing, updateItem, dbCollection);
            }

            // remove obsolete items
            foreach (var dbItem in dbHash.Values)
                RemoveElement(context, dbItem, dbCollection);
        }

        private void AddElement<T>(DbContext context, T existing, object updateItem, object dbCollection)
        {
            if (!_isOwned)
                AttachAndReloadEntity(context, updateItem);

            dbCollection.GetType().GetMethod("Add").Invoke(dbCollection, new[] {updateItem});

            AttachCyclicNavigationProperty(context, existing, updateItem);
        }

        private void UpdateElement<T>(DbContext context, T existing, object updateItem, object dbItem)
        {
            if (!_isOwned)
                return;

            UpdateValuesWithConcurrencyCheck(context, updateItem, dbItem);

            AttachCyclicNavigationProperty(context, existing, updateItem);

            foreach (var childMember in Members)
                childMember.Update(context, dbItem, updateItem);
        }

        private void RemoveElement(DbContext context, object dbItem, object dbCollection)
        {
            dbCollection.GetType().GetMethod("Remove").Invoke(dbCollection, new[] { dbItem });

            if (_isOwned)
                context.Set(ObjectContext.GetObjectType(dbItem.GetType())).Remove(dbItem);
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
