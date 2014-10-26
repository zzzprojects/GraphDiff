using System;

namespace RefactorThis.GraphDiff.Aggregates.Attributes
{
    /// <summary>Marks this property as associated by the parent type or by the chosen AggregateType</summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class AssociatedAttribute : AggregateDefinitionAttribute
    {
    }
}
