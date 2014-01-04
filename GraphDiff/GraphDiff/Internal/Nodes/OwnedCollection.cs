using System.Reflection;

namespace RefactorThis.GraphDiff.Internal.Nodes
{
    internal class OwnedCollection : AMember
    {
        internal OwnedCollection(AMember parent, PropertyInfo accessor)
                : base(parent, accessor)
        {
        }
    }
}