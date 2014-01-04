using System.Reflection;

namespace RefactorThis.GraphDiff.Internal.Nodes
{
    internal class OwnedEntityMember : AMember
    {
        internal OwnedEntityMember(AMember parent, PropertyInfo accessor)
                : base(parent, accessor)
        {
        }
    }
}
