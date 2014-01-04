using System.Reflection;

namespace RefactorThis.GraphDiff.Internal.Members
{
    internal class AssociatedCollection : ACollectionMember
    {
        internal AssociatedCollection(AMember parent, PropertyInfo accessor)
                : base(parent, accessor)
        {
        }
    }
}