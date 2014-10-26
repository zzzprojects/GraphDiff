using System.Reflection;
using RefactorThis.GraphDiff.Internal.Graph;

namespace RefactorThis.GraphDiff.Internal.GraphBuilders
{
    internal static class GraphNodeFactory
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
