using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RefactorThis.GraphDiff.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class AssociatedAttribute : AggregateDefinitionAttribute
    {
        public AssociatedAttribute()
        {
        }
    }
}
