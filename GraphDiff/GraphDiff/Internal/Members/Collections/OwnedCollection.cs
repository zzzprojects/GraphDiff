using System.Reflection;

namespace RefactorThis.GraphDiff.Internal.Members.Collections
{
    internal class OwnedCollection : ACollectionMember
    {
        internal OwnedCollection(AMember parent, PropertyInfo accessor)
                : base(parent, accessor)
        {
        }
    }
}