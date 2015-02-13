using System.Data.Entity;
using RefactorThis.GraphDiff.Tests.Models;
using RefactorThis.GraphDiff.Tests.Tests;

namespace RefactorThis.GraphDiff.Tests
{
    public class TestDbContext : DbContext
    {
        public IDbSet<TestNode> Nodes { get; set; }
        public IDbSet<TestNodeWithBaseReference> NodesWithReference { get; set; }

        public IDbSet<NodeGroup> NodeGroups { get; set; }

        public IDbSet<OneToOneOwnedModel> OneToOneOwnedModels { get; set; }
        public IDbSet<OneToOneAssociatedModel> OneToOneAssociatedModels { get; set; }
        public IDbSet<OneToManyAssociatedModel> OneToManyAssociatedModels { get; set; }
        public IDbSet<OneToManyOwnedModel> OneToManyOwnedModels { get; set; }

        public IDbSet<MultiKeyModel>  MultiKeyModels { get; set; }

        public IDbSet<RootEntity> RootEntities { get; set; }

        public IDbSet<RequiredAssociate> RequiredAssociates { get; set; }
        public IDbSet<GuidEntity> GuidKeyModels { get; set; }
        public IDbSet<InternalKeyModel> InternalKeyModels { get; set; }
        public IDbSet<NullableKeyModel> NullableKeyModels { get; set; }

        public IDbSet<PKFKModelPrimary> PKFKModelPrimaries { get; set; }
        public IDbSet<PKFKModelSecondary> PKFKModelSecondaries { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // second tier mappings

            modelBuilder.Entity<TestNode>().HasOptional(p => p.OneToOneAssociated).WithOptionalPrincipal(p => p.OneParent);
            modelBuilder.Entity<TestNode>().HasMany(p => p.OneToManyAssociated).WithOptional(p => p.OneParent);
            modelBuilder.Entity<TestNode>().HasOptional(p => p.OneToOneOwned).WithRequired(p => p.OneParent).WillCascadeOnDelete();
            modelBuilder.Entity<TestNode>().HasMany(p => p.OneToManyOwned).WithRequired(p => p.OneParent).WillCascadeOnDelete();

            modelBuilder.Entity<GroupedTestNode>().HasOptional(g => g.One).WithOptionalDependent(g => g.Two).WillCascadeOnDelete(false);

            // third tier mappings

            modelBuilder.Entity<OneToManyOwnedModel>().HasOptional(p => p.OneToManyOneToOneAssociated).WithOptionalPrincipal(p => p.OneParent);
            modelBuilder.Entity<OneToManyOwnedModel>().HasMany(p => p.OneToManyOneToManyAssociated).WithOptional(p => p.OneParent);
            modelBuilder.Entity<OneToManyOwnedModel>().HasOptional(p => p.OneToManyOneToOneOwned).WithRequired(p => p.OneParent).WillCascadeOnDelete();
            modelBuilder.Entity<OneToManyOwnedModel>().HasMany(p => p.OneToManyOneToManyOwned).WithRequired(p => p.OneParent).WillCascadeOnDelete();

            modelBuilder.Entity<OneToOneOwnedModel>().HasOptional(p => p.OneToOneOneToOneAssociated).WithOptionalPrincipal(p => p.OneParent);
            modelBuilder.Entity<OneToOneOwnedModel>().HasMany(p => p.OneToOneOneToManyAssociated).WithOptional(p => p.OneParent);
            modelBuilder.Entity<OneToOneOwnedModel>().HasOptional(p => p.OneToOneOneToOneOwned).WithRequired(p => p.OneParent).WillCascadeOnDelete();
            modelBuilder.Entity<OneToOneOwnedModel>().HasMany(p => p.OneToOneOneToManyOwned).WithRequired(p => p.OneParent).WillCascadeOnDelete();

            modelBuilder.Entity<RootEntity>().HasOptional(c => c.Target).WithMany(c => c.Sources);

            // Guid mappings

            modelBuilder.Entity<GuidTestNode>().HasOptional(p => p.OneToOneOwned).WithRequired(p => p.OneParent);

            // Internal mappings

            modelBuilder.Entity<InternalKeyModel>().HasKey(i => i.Id);
            modelBuilder.Entity<InternalKeyAssociate>().HasKey(i => i.Id);

            modelBuilder.Entity<InternalKeyModel>()
                    .HasMany(ikm => ikm.Associates)
                    .WithRequired(ikm => ikm.Parent);

            modelBuilder.Entity<PKFKModelPrimary>().HasOptional(p => p.Secondary).WithRequired();
        }

        public TestDbContext() : base("GraphDiff") {}
    }
}
