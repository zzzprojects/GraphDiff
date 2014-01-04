using System.Reflection;

namespace RefactorThis.GraphDiff.Internal.Nodes
{
    internal class AssociatedEntity : AMember
    {
        internal AssociatedEntity(AMember parent, PropertyInfo accessor)
                : base(parent, accessor)
        {
        }
    }
}
