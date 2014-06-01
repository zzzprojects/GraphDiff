using RefactorThis.GraphDiff.Attributes;
using RefactorThis.GraphDiff.Internal.Graph;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Runtime.Caching;
using RefactorThis.GraphDiff.Internal.Caching;

namespace RefactorThis.GraphDiff.Internal
{
    internal class AggregateRegister
    {
        private readonly ICacheProvider _cache;

        public AggregateRegister(ICacheProvider cache)
        {
            _cache = cache;
        }

        public void Register(Type childType, Type aggregate, bool isAssociation)
        {
        }

        public GraphNode GetEntityGraph(Type type)
        {
            return _cache.GetOrAdd<GraphNode>(typeof(AggregateRegister).FullName + type.FullName, () =>
            {
                var node = new GraphNode();
                BuildEntityGraph(node, type);
                return node;
            });
        }

        private void BuildEntityGraph(GraphNode parent, Type type)
        {
            var properties = type.GetProperties()
                .Select(p => new
                {
                    Accessor = p,
                    IsOwned = p.GetCustomAttributes(typeof(OwnedAttribute), true).Any(),
                    IsAssociated = p.GetCustomAttributes(typeof(AssociatedAttribute), true).Any()
                })
                .Where(p => p.IsOwned || p.IsAssociated);

            foreach (var property in properties)
            {
                var propertyType = property.Accessor.PropertyType;
                bool isCollection = false;

                // if collection
                var genericType = propertyType
                    .GetInterfaces()
                    .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    .Select(t => t.GetGenericArguments().First())
                    .FirstOrDefault();

                if (genericType != null)
                {
                    isCollection = true;
                    propertyType = genericType;
                }
                else if (propertyType.IsArray)
                {
                    isCollection = true;
                    propertyType = type.GetElementType();
                }

                var node = GraphNodeFactory.Create(parent, property.Accessor, isCollection, property.IsOwned);
                parent.Members.Push(node);

                if (property.IsOwned)
                {
                    BuildEntityGraph(node, propertyType);
                }
            }        
        }
    }
}
