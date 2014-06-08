using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RefactorThis.GraphDiff.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class AggregateRootAttribute : Attribute
    {
        public AggregateRootAttribute()
        {
        }
    }
}
