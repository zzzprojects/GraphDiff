using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RefactorThis.GraphDiff.Tests.Models
{

    // ====================================
    // First tier models
    // ====================================

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

        //public ManyToOneModel ManyToOneOwned { get; set; }
        //public ManyToOneModel ManyToOneAssociated { get; set; }

        //public ICollection<ManyToManyModel> ManyToManyAssociated { get; set; }
    }

    public class NodeGroup : Entity
    {
        public List<GroupedTestNode> Members { get; set; }
    }

    public class GroupedTestNode : TestNode
    {
        public GroupedTestNode One { get; set; }

        public GroupedTestNode Two { get; set; }

        public NodeGroup Group { get; set; }
    }

    public class TestChildNode : TestNode
    {
    }

    public class TestNodeWithBaseReference : Entity
    {
        public TestNode OneToOneOwnedBase { get; set; }
    }

    public class RootEntity : Entity
    {
        [Required]
        public RequiredAssociate RequiredAssociate { get; set; }

        public List<RootEntity> Sources { get; set; }

        public int? TargetId { get; set; }
        public RootEntity Target { get; set; }
    }

    public class RequiredAssociate : Entity
    {
        public List<RootEntity> RootEntities { get; set; }
    }

    public class MultiKeyModel
    {
        [Key]
        [Column(Order=1)]
        public string KeyPart1 { get; set; }

        [Key]
        [Column(Order = 2)]
        public string KeyPart2 { get; set; }

        public string Title { get; set; }
        public DateTime Date { get; set; }
    }

    public class InternalKeyModel
    {
        [Key]
        internal int Id { get; set; }

        internal List<InternalKeyAssociate> Associates { get; set; }
    }

    public class InternalKeyAssociate
    {
        [Key]
        internal int Id { get; set; }

        internal InternalKeyModel Parent { get; set; }
    }

    public class NullableKeyModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid? Id { get; set; }
    }

    public class TestNodeForIListMultiAddition : Entity
    {
        // If you change this to ICollection, then test TestIListOwnedCollectionAdditionDoesNotMultiAdd will pass
        public IList<TestSubNodeForIListMultiAddition> SubNodes { get; set; }
    }

    public class TestSubNodeForIListMultiAddition
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column(Order = 1)]
        public int TestNodeForIListMultiAdditionId { get; set; }
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column(Order = 2)]
        public int OtherKeyId { get; set; }

        public string Title { get; set; }
    }

    // ====================================
    // Second tier models
    // ====================================
    // Different classes for each as they will be mapped to DB tables with specific relations/constraints by EF
    // model builder. names may seem confusing but allow us to ensure we have tests for all scenarios.

    public class OneToOneAssociatedModel : Entity
    {
        public TestNode OneParent { get; set; }
    }

    public class OneToOneOwnedModel : Entity
    {
        public TestNode OneParent { get; set; }
        public ICollection<OneToOneOneToManyOwnedModel> OneToOneOneToManyOwned { get; set; }
        public OneToOneOneToOneOwnedModel OneToOneOneToOneOwned { get; set; }
        public ICollection<OneToOneOneToManyAssociatedModel> OneToOneOneToManyAssociated { get; set; }
        public OneToOneOneToOneAssociatedModel OneToOneOneToOneAssociated { get; set; }
    }

    public class OneToManyAssociatedModel : Entity
    {
        public TestNode OneParent { get; set; }
    }

    public class OneToManyOwnedModel : Entity
    {
        public TestNode OneParent { get; set; }
        public ICollection<OneToManyOneToManyOwnedModel> OneToManyOneToManyOwned { get; set; }
        public OneToManyOneToOneOwnedModel OneToManyOneToOneOwned { get; set; }
        public ICollection<OneToManyOneToManyAssociatedModel> OneToManyOneToManyAssociated { get; set; }
        public OneToManyOneToOneAssociatedModel OneToManyOneToOneAssociated { get; set; }
    }

    /*
    public class ManyToOneModel : Entity
    {
        public ICollection<TestNode> ManyParents { get; set; }
    }

    public class ManyToManyModel : Entity
    {
        public ICollection<TestNode> ManyParents { get; set; }
    }
     * */

    // ====================================
    // Third tier models
    // ====================================

    public class OneToManyOneToOneOwnedModel : Entity
    {
        public OneToManyOwnedModel OneParent { get; set; }
    }

    public class OneToManyOneToManyOwnedModel : Entity
    {
        public OneToManyOwnedModel OneParent { get; set; }
    }

    public class OneToManyOneToOneAssociatedModel : Entity
    {
        public OneToManyOwnedModel OneParent { get; set; }
    }

    public class OneToManyOneToManyAssociatedModel : Entity
    {
        public OneToManyOwnedModel OneParent { get; set; }
    }

    public class OneToOneOneToOneOwnedModel : Entity
    {
        public OneToOneOwnedModel OneParent { get; set; }
    }

    public class OneToOneOneToManyOwnedModel : Entity
    {
        public OneToOneOwnedModel OneParent { get; set; }
    }

    public class OneToOneOneToOneAssociatedModel : Entity
    {
        public OneToOneOwnedModel OneParent { get; set; }
    }

    public class OneToOneOneToManyAssociatedModel : Entity
    {
        public OneToOneOwnedModel OneParent { get; set; }
    }
}
