using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RefactorThis.GraphDiff.Attributes
{
    public class AggregateDefinitionAttribute : Attribute
    {
        /// <summary>
        /// The aggregate type who the ownership/association refers to.
        /// </summary>
        public Type AggregateType { get; set; }
    }
}
