using System.Collections.Generic;
using System.Data.Entity;
using System.Reflection;

namespace RefactorThis.GraphDiff.Internal.Members
{
    internal abstract class AMember
    {
        internal AMember Parent { get; private set; }
        internal Stack<AMember> Members { get; private set; }
        internal PropertyInfo Accessor { get; private set; }

        internal string IncludeString
        {
            get
            {
                var ownIncludeString = Accessor != null ? Accessor.Name : null;
                return Parent != null && Parent.IncludeString != null
                        ? Parent.IncludeString + "." + ownIncludeString
                        : ownIncludeString;
            }
        }

        internal AMember(AMember parent, PropertyInfo accessor)
        {
            Accessor = accessor;
            Members = new Stack<AMember>();
            Parent = parent;
        }

#warning mark as abstract as soon as every child class implements the method
        internal virtual void Update<T>(DbContext context, T existing, T entity) where T : class, new()
        {
            DbContextExtensions.RecursiveGraphUpdate(context, existing, entity, this);
        }
    }
}
