using System.Reflection;

namespace RefactorThis.GraphDiff.Internal.Members
{
    internal class AssociatedEntity : AEntityMember
    {
        internal AssociatedEntity(AMember parent, PropertyInfo accessor)
                : base(parent, accessor)
        {
        }
    }
}
