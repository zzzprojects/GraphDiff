using System.Reflection;

namespace RefactorThis.GraphDiff.Internal.Nodes
{
    internal class OwnedEntity : AMember
    {
        internal OwnedEntity(AMember parent, PropertyInfo accessor)
                : base(parent, accessor)
        {
        }
    }
}
