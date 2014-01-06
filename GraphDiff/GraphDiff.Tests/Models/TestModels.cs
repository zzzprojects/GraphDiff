using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RefactorThis.GraphDiff.Tests.Models
{
    public class Entity
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(128)]
        public string Title { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; }
    }

    public class TestNode : Entity
    {
        public OneToOneOwnedModel OneToOneOwned { get; set; }
        public OneToOneAssociatedModel OneToOneAssociated { get; set; }

        public ICollection<OneToManyOwnedModel> OneToManyOwned { get; set; }
        public ICollection<OneToManyAssociatedModel> OneToManyAssociated { get; set; }

        public ManyToOneModel ManyToOneOwned { get; set; }
        public ManyToOneModel ManyToOneAssociated { get; set; }

        public ICollection<ManyToManyModel> ManyToManyAssociated { get; set; }
    }

    public class OneToOneAssociatedModel : Entity
    {
        public TestNode OneParent { get; set; }
    }

    public class OneToOneOwnedModel : Entity
    {
        public TestNode OneParent { get; set; }
    }

    public class OneToManyAssociatedModel : Entity
    {
        public TestNode OneParent { get; set; }
    }

    public class OneToManyOwnedModel : Entity
    {
        public TestNode OneParent { get; set; }
    }

    public class ManyToOneModel : Entity
    {
        public ICollection<TestNode> ManyParents { get; set; }
    }

    public class ManyToManyModel : Entity
    {
        public ICollection<TestNode> ManyParents { get; set; }
    }
}
