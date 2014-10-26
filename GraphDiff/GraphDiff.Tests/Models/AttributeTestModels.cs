using System.Collections.Generic;
using RefactorThis.GraphDiff.Aggregates.Attributes;

namespace RefactorThis.GraphDiff.Tests.Models
{
    public class AttributeTest
    {
        public int Id { get; set; }
        public string Title { get; set; }

        [Owned]
        public ICollection<AttributeTestOneToManyOwned> OneToManyOwned { get; set; }

        [Associated]
        public ICollection<AttributeTestOneToManyAssociated> OneToManyAssociated { get; set; }
    }

    public class CircularAttributeTest
    {
        public int Id { get; set; }

        [Owned]
        public CircularAttributeTest Parent { get; set; }
    }

    public class SharedModelAttributeTest
    {
        public int Id { get; set; }
        public string Title { get; set; }

        [Owned]
        public ICollection<AttributeTestOneToManyOwned> OneToManyOwned { get; set; }

        [Associated(AggregateType = typeof(AttributeTest))]
        public ICollection<AttributeTestOneToManyAssociated> OneToManyAssociated { get; set; }
    }

    public class AttributeTestOneToManyOwned
    {
        public int Id { get; set; }
        public string Title { get; set; }

        [Owned(AggregateType = typeof(AttributeTest))]
        public AttributeTestOneToManyToOneOwned AttributeTestOneToManyToOneOwned { get; set; }

        [Owned(AggregateType = typeof(SharedModelAttributeTest))]
        public AttributeTestOneToManyToOneOwned SharedModelTesting { get; set; }

        [Associated]
        public AttributeTestOneToManyToOneAssociated AttributeTestOneToManyToOneAssociated { get; set; }
    }

    public class AttributeTestOneToManyAssociated
    {
        public int Id { get; set; }
        public string Title { get; set; }
    }

    public class AttributeTestOneToManyToOneOwned
    {
        public int Id { get; set; }
        public string Title { get; set; }
    }

    public class AttributeTestOneToManyToOneAssociated
    {
        public int Id { get; set; }
        public string Title { get; set; }
    }
}