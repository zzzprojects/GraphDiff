﻿using System;
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

            // if (dbValue != null) // make an error throw or not?
            foreach (var childMember in Members)
                childMember.Update(context, dbValue, newValue);
        }

        private object CreateNewPersistedEntity<T>(DbContext context, T existing, object newValue) where T : class, new()
        {
            // TBD_79ad60a7-8091-4d0c-b5de-7373f3b8cedf: Could be accepted with an option. Otherwise, we cannot allow people by default to provide multiple entity with same ID if it's not the same.
            //var local = context.Set(Accessor.PropertyType).Local;
            //foreach (var entity in local)
            //{
            //    if (entity.Equals(newValue))
            //    {
            //        SetValue(existing, entity);
            //        return entity;
            //    }
            //}

            var instance = Activator.CreateInstance(newValue.GetType());
            SetValue(existing, instance);
            context.Set(Accessor.PropertyType).Add(instance);
            UpdateValuesWithConcurrencyCheck(context, newValue, instance);
            return instance;
        }
    }
}
