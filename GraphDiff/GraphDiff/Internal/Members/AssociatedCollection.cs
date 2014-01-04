﻿using System.Reflection;

namespace RefactorThis.GraphDiff.Internal.Members
{
    internal class AssociatedCollection : AMember
    {
        internal AssociatedCollection(AMember parent, PropertyInfo accessor)
                : base(parent, accessor)
        {
        }
    }
}