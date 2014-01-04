using System.Reflection;

namespace RefactorThis.GraphDiff.Internal.Members
{
    internal class OwnedEntity : AEntityMember
    {
        internal OwnedEntity(AMember parent, PropertyInfo accessor)
                : base(parent, accessor)
        {
        }
    }
}
