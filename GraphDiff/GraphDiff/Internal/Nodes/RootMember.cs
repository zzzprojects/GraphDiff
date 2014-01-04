using System.Reflection;

namespace RefactorThis.GraphDiff.Internal.Nodes
{
    internal class RootMember : AMember
    {
        internal RootMember(AMember parent, PropertyInfo accessor)
                : base(parent, accessor)
        {
        }
    }
}