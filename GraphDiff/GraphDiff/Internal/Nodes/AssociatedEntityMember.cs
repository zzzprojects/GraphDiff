using System.Reflection;

namespace RefactorThis.GraphDiff.Internal.Nodes
{
    internal class AssociatedEntityMember : AMember
    {
        internal AssociatedEntityMember(AMember parent, PropertyInfo accessor)
                : base(parent, accessor)
        {
        }
    }
}
