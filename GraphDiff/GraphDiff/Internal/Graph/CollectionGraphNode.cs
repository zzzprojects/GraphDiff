using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Reflection;

namespace RefactorThis.GraphDiff.Internal.Graph
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

            var dbHash = dbCollection.Cast<object>().ToDictionary(item => CreateEntityKey(context, item));

            // Iterate through the elements from the updated graph and try to match them against the db graph
            var updateList = updateValues.OfType<object>().ToList();
            for (int i = 0; i < updateList.Count; i++)
            {
                var updateItem = updateList[i];
                var key = CreateEntityKey(context, updateItem);

                // try to find item with same key in db collection
                object dbItem;
                if (dbHash.TryGetValue(key, out dbItem))
                {
                    UpdateElement(context, existing, updateItem, dbItem);
                    dbHash.Remove(key);
                }
                else
                {
                    updateList[i] = AddElement(context, existing, updateItem, dbCollection);
                }
            }

            if (CheckAllowDelete(this))
            {
                // remove obsolete items
                foreach (var dbItem in dbHash.Values)
                {
                    RemoveElement(context, dbItem, dbCollection);
                }
            }
        }
        
        private static bool CheckAllowDelete(GraphNode node)
        {
            if (node.AllowDelete.HasValue)
            {
                return node.AllowDelete.Value;
            }
            else if (node.Parent != null)
            {
                return CheckAllowDelete(node.Parent);
            }
            else
            {
                return true;
            }
        }

        private object AddElement<T>(DbContext context, T existing, object updateItem, object dbCollection)
        {
            if (!_isOwned)
            {
                updateItem = AttachAndReloadAssociatedEntity(context, updateItem);
            }
            else if (context.Entry(updateItem).State == EntityState.Detached)
            {
                var entityType = ObjectContext.GetObjectType(updateItem.GetType());
                var instance = CreateEmptyEntityWithKey(context, updateItem);

                context.Set(entityType).Add(instance);
                context.Entry(instance).CurrentValues.SetValues(updateItem);

                foreach (var childMember in Members)
                {
                    childMember.Update(context, instance, updateItem);
                }

                updateItem = instance;
            }

            dbCollection.GetType().GetMethod("Add").Invoke(dbCollection, new[] {updateItem});

            AttachCyclicNavigationProperty(context, existing, updateItem);

            return updateItem;
        }

        private void UpdateElement<T>(DbContext context, T existing, object updateItem, object dbItem)
        {
            if (!_isOwned) return;

            UpdateValuesWithConcurrencyCheck(context, updateItem, dbItem);

            AttachCyclicNavigationProperty(context, existing, updateItem);

            foreach (var childMember in Members)
            {
                childMember.Update(context, dbItem, updateItem);
            }
        }

        private void RemoveElement(DbContext context, object dbItem, object dbCollection)
        {
            dbCollection.GetType().GetMethod("Remove").Invoke(dbCollection, new[] { dbItem });

            AttachRequiredNavigationProperties(context, dbItem, dbItem);

            if (_isOwned)
            {
                context.Set(ObjectContext.GetObjectType(dbItem.GetType())).Remove(dbItem);
            }
        }

        private IEnumerable CreateMissingCollection(object existing, Type elementType)
        {
            var collectionType = !Accessor.PropertyType.IsInterface ? Accessor.PropertyType : typeof(List<>).MakeGenericType(elementType);
            var collection = (IEnumerable)Activator.CreateInstance(collectionType);
            SetValue(existing, collection);
            return collection;
        }

        protected override IEnumerable<string> GetRequiredNavigationPropertyIncludes(DbContext context)
        {
            if (_isOwned)
            {
                return base.GetRequiredNavigationPropertyIncludes(context);
            }

            return Accessor != null
                    ? GetRequiredNavigationPropertyIncludes(context, GetCollectionElementType(), IncludeString)
                    : new string[0];
        }

        private Type GetCollectionElementType()
        {
            if (Accessor.PropertyType.IsArray)
            {
                return Accessor.PropertyType.GetElementType();
            }

            if (Accessor.PropertyType.IsGenericType)
            {
                return Accessor.PropertyType.GetGenericArguments()[0];
            }

            throw new InvalidOperationException("GraphDiff requires the collection to be either IEnumerable<T> or T[]");
        }
    }
}
