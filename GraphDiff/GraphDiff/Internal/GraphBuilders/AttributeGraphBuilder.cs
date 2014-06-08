using RefactorThis.GraphDiff.Attributes;
using RefactorThis.GraphDiff.Internal.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
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
            return t.GetCustomAttributes(typeof(AggregateRootAttribute), true).Any();
        }

        public GraphNode BuildGraph<T>()
        {
            var node = new GraphNode();
            BuildEntityGraph(node, typeof(T));
            return node;
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
