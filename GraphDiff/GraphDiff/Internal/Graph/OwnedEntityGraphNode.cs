using System;
using System.Data.Entity;
using System.Reflection;

namespace RefactorThis.GraphDiff.Internal.Graph
{
    internal class OwnedEntityGraphNode : GraphNode
    {
        internal OwnedEntityGraphNode(GraphNode parent, PropertyInfo accessor)
                : base(parent, accessor)
        {
        }

        public override void Update<T>(DbContext context, T persisted, T updating)
        {
            var dbValue = GetValue<object>(persisted);
            var newValue = GetValue<object>(updating);

            if (dbValue == null && newValue == null)
                return;

            // Merging options
            // 1. No new value, set value to null. entity will be removed if cascade rules set.
            // 2. If new value is same as old value lets update the members
            // 3. Otherwise new value is set and we don't care about old dbValue, so create a new one.
            if (newValue == null)
            {
                SetValue(persisted, null);
                return;
            }
            
            if (dbValue != null && IsKeyIdentical(context, newValue, dbValue))
                UpdateValuesWithConcurrencyCheck(context, newValue, dbValue);
            else
                dbValue = CreateNewPersistedEntity(context, persisted, newValue);

            AttachCyclicNavigationProperty(context, persisted, newValue);

            foreach (var childMember in Members)
                childMember.Update(context, dbValue, newValue);
        }

        private object CreateNewPersistedEntity<T>(DbContext context, T existing, object newValue) where T : class, new()
        {
            var instance = Activator.CreateInstance(Accessor.PropertyType);
            SetValue(existing, instance);
            context.Set(Accessor.PropertyType).Add(instance);
            UpdateValuesWithConcurrencyCheck(context, newValue, instance);
            return instance;
        }
    }
}
