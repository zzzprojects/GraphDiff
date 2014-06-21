using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RefactorThis.GraphDiff.Attributes
{
    /// <summary>
    /// Marks this property as associated by the parent type or by the chosen AggregateType
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class AssociatedAttribute : AggregateDefinitionAttribute
    {
        public AssociatedAttribute()
        {
        }
    }
}
