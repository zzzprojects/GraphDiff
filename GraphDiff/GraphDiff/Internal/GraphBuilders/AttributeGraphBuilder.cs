using RefactorThis.GraphDiff.Attributes;
using RefactorThis.GraphDiff.Internal.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace RefactorThis.GraphDiff.Internal.GraphBuilders
{
    internal interface IAttributeGraphBuilder
    {
        bool CanBuild(Type t);
        GraphNode BuildGraph<T>();
    }

    internal class AttributeGraphBuilder : IAttributeGraphBuilder
    {
        public bool CanBuild(Type t)
        {
            // any properties have an aggregate definition attribute?
            return t.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .SelectMany(p => p.GetCustomAttributes(true))
                .Where(p => p is OwnedAttribute || p is AssociatedAttribute)
                .Any();
        }

        public GraphNode BuildGraph<T>()
        {
            var node = new GraphNode();
            var visited = new Dictionary<string, bool>();
            BuildEntityGraph<T>(node, typeof(T), visited);
            return node;
        }

        private void BuildEntityGraph<AggregateT>(GraphNode parent, Type type, Dictionary<string, bool> visited)
        {
            var properties = type.GetProperties()
                .Select(p => new
                {
                    Accessor = p,
                    IsOwned = p.GetCustomAttributes(typeof(OwnedAttribute), true)
                        .Cast<OwnedAttribute>()
                        .Where(attr => attr.AggregateType == null || attr.AggregateType == typeof(AggregateT))
                        .Any(),
                    IsAssociated = p.GetCustomAttributes(typeof(AssociatedAttribute), true)
                        .Cast<AssociatedAttribute>()
                        .Where(attr => attr.AggregateType == null || attr.AggregateType == typeof(AggregateT))
                        .Any()
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
                    BuildEntityGraph<AggregateT>(node, propertyType, visited);
                }
            }
        }

        private static void SetVisited(Dictionary<string, bool> visited, GraphNode node)
        {
            visited.Add(node.GetUniqueKey(), true);
        }

        private static bool IsVisited(Dictionary<string, bool> visited, GraphNode node)
        {
            bool val = false;
            visited.TryGetValue(node.GetUniqueKey(), out val);
            return val;
        }
    }
}
