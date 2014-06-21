using RefactorThis.GraphDiff.Internal.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace RefactorThis.GraphDiff.Internal.GraphBuilders
{
    internal class GraphNodeFactory
    {
        public static GraphNode Create(GraphNode parent, PropertyInfo accessor, bool isCollection, bool isOwned)
        {
            if (isCollection)
            {
                return new CollectionGraphNode(parent, accessor, isOwned);
            }

            return isOwned
                ? new OwnedEntityGraphNode(parent, accessor)
                : (GraphNode)new AssociatedEntityGraphNode(parent, accessor);
        }
    }
}
