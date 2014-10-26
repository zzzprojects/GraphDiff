using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RefactorThis.GraphDiff.Aggregates.Attributes;
using RefactorThis.GraphDiff.Internal.Graph;

namespace RefactorThis.GraphDiff.Internal.GraphBuilders
{
    internal interface IAttributeGraphBuilder
    {
        bool CanBuild<T>();
        GraphNode BuildGraph<T>();
    }

    internal class AttributeGraphBuilder : IAttributeGraphBuilder
    {
        public bool CanBuild<T>()
        {
            // any properties have an aggregate definition attribute?
            return typeof (T).GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                    .SelectMany(p => p.GetCustomAttributes(true))
                    .Any(p => p is OwnedAttribute || p is AssociatedAttribute);
        }

        public GraphNode BuildGraph<T>()
        {
            var node = new GraphNode();
            var visited = new Dictionary<string, bool>();
            BuildEntityGraph<T>(node, typeof(T), visited);
            return node;
        }

        private static void BuildEntityGraph<TAggregate>(GraphNode parent, Type type, Dictionary<string, bool> visited)
        {
            var properties = type.GetProperties()
                .Select(p => new
                {
                    Accessor = p,
                    IsOwned = p.GetCustomAttributes(typeof(OwnedAttribute), true)
                        .Cast<OwnedAttribute>()
                        .Any(attr => attr.AggregateType == null || attr.AggregateType == typeof(TAggregate)),
                    IsAssociated = p.GetCustomAttributes(typeof(AssociatedAttribute), true)
                        .Cast<AssociatedAttribute>()
                        .Any(attr => attr.AggregateType == null || attr.AggregateType == typeof(TAggregate))
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
                    if (IsVisited(visited, node))
                    {
                        throw new NotSupportedException("Attribute mapping does not support circular graphs");
                    }
                    SetVisited(visited, node);
                    BuildEntityGraph<TAggregate>(node, propertyType, visited);
                }
            }
        }

        private static void SetVisited(Dictionary<string, bool> visited, GraphNode node)
        {
            visited.Add(node.GetUniqueKey(), true);
        }

        private static bool IsVisited(Dictionary<string, bool> visited, GraphNode node)
        {
            bool val;
            visited.TryGetValue(node.GetUniqueKey(), out val);
            return val;
        }
    }
}
