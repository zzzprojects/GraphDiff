using System;
using System.Reflection;

namespace RefactorThis.GraphDiff.Internal.Graph
{
    internal class OwnedEntityGraphNode : GraphNode
    {
        internal OwnedEntityGraphNode(GraphNode parent, PropertyInfo accessor)
                : base(parent, accessor)
        {
            ThrowIfCollectionType(accessor, "owned");
        }

        public override void Update<T>(IChangeTracker changeTracker, IEntityManager entityManager, T persisted, T updating)
        {
            var dbValue = GetValue<object>(persisted);
            var newValue = GetValue<object>(updating);

            if (dbValue == null && newValue == null)
            {
                return;
            }

            // Merging options
            // 1. No new value, set value to null. entity will be removed if cascade rules set.
            // 2. If new value is same as old value lets update the members
            // 3. Otherwise new value is set and we don't care about old dbValue, so create a new one.
            if (newValue == null)
            {
                SetValue(persisted, null);
                return;
            }

            if (dbValue != null && entityManager.AreKeysIdentical(newValue, dbValue))
            {
                changeTracker.UpdateItem(newValue, dbValue, true);
            }
            else
            {
                dbValue = CreateNewPersistedEntity(changeTracker, persisted, newValue);
            }

            changeTracker.AttachCyclicNavigationProperty(persisted, newValue, GetMappedNaviationProperties());

            foreach (var childMember in Members)
            {
                childMember.Update(changeTracker, entityManager, dbValue, newValue);
            }
        }

        private object CreateNewPersistedEntity<T>(IChangeTracker changeTracker, T existing, object newValue) where T : class
        {
            var instance = Activator.CreateInstance(newValue.GetType(), true);
            SetValue(existing, instance);
            changeTracker.AddItem(instance);
            changeTracker.UpdateItem(newValue, instance, true);
            return instance;
        }
    }
}
