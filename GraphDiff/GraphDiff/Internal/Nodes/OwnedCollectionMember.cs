using System.Reflection;

namespace RefactorThis.GraphDiff.Internal.Nodes
{
    internal class OwnedCollectionMember : AMember
    {
        internal OwnedCollectionMember(AMember parent, PropertyInfo accessor)
                : base(parent, accessor)
        {
        }
    }
}