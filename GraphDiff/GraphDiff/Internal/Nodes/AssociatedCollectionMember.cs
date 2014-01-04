using System.Reflection;

namespace RefactorThis.GraphDiff.Internal.Nodes
{
    internal class AssociatedCollectionMember : AMember
    {
        internal AssociatedCollectionMember(AMember parent, PropertyInfo accessor)
                : base(parent, accessor)
        {
        }
    }
}