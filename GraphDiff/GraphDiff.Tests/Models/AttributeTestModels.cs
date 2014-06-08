using RefactorThis.GraphDiff.Attributes;
using System.Collections.Generic;

namespace RefactorThis.GraphDiff.Tests.Models
{
    [AggregateRoot]
    public class AttributeTest
    {
        public int Id { get; set; }
        public string Title { get; set; }

        [Owned]
        public ICollection<AttributeTestOneToManyOwned> OneToManyOwned { get; set; }

        [Associated]
        public ICollection<AttributeTestOneToManyAssociated> OneToManyAssociated { get; set; }
    }

    public class AttributeTestOneToManyOwned
    {
        public int Id { get; set; }
        public string Title { get; set; }

        [Owned]
        public AttributeTestOneToManyToOneOwned AttributeTestOneToManyToOneOwned { get; set; }

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