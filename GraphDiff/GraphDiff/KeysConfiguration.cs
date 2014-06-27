using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace RefactorThis.GraphDiff
{
    /// <summary>
    /// Defines custom entity keys to use during merge instead of primary key
    /// </summary>
    public sealed class KeysConfiguration
    {
        private class PropertyInfoExpressionVisitor : ExpressionVisitor
        {
            public PropertyInfo PropertyInfo { get; private set; }

            protected override Expression VisitMember(MemberExpression node)
            {
                var pi = node.Member as PropertyInfo;
                if (pi != null)
                    PropertyInfo = pi;
                return base.VisitMember(node);
            }
        }

        private readonly Dictionary<Type, IList<PropertyInfo>> _entityKeys = new Dictionary<Type, IList<PropertyInfo>>();

        /// <summary>
        /// Defines a key configuration for an entity type.
        /// Be careful about your key, you have to ensure uniqueness.
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="key">Path to entity key properties. Ensure that your key is unique.</param>
        /// <returns>Keys configuration to chain call</returns>
        public KeysConfiguration ForEntity<T>(params Expression<Func<T, object>>[] key)
        {
            if (_entityKeys.ContainsKey(typeof(T)))
                throw new InvalidOperationException("A key configuration is already defined for entity type" + typeof(T).Name);
            var propertyInfos = key.Select(e => GetPropertyInfo(e));
            _entityKeys.Add(typeof(T), propertyInfos.ToList());
            return this;
        }

        private static PropertyInfo GetPropertyInfo<TEntity>(Expression<Func<TEntity, object>> expression)
        {
            var visitor = new PropertyInfoExpressionVisitor();
            visitor.Visit(expression);
            return visitor.PropertyInfo;
        }

        internal IList<PropertyInfo> GetEntityKey(Type entityType)
        {
            IList<PropertyInfo> result;
            if (_entityKeys.TryGetValue(entityType, out result))
                return result;
            else
                return null;
        }

        internal bool HasConfigurationFor(Type entityType)
        {
            return _entityKeys.ContainsKey(entityType);
        }
    }
}
