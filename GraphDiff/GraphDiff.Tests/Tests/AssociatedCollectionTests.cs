using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RefactorThis.GraphDiff.Tests.Models;

namespace RefactorThis.GraphDiff.Tests.Tests
{
    [TestClass]
    public class AssociatedCollectionTests : TestBase
    {
        [TestMethod]
        public void TestAsAssociated()
        {
            var targetRequired = new RequiredAssociate();
            var source = new RootEntity();
            var target = new RootEntity();

            using (var context = new AssociationsContext())
            {
                context.RequiredAssociates.Add(targetRequired);
                
                context.RootEntities.Add(source);
                source.RequiredAssociate = targetRequired;

                context.RootEntities.Add(target);
                target.RequiredAssociate = targetRequired;

                context.SaveChanges();
            }

            target.Sources = new List<RootEntity> {source};

            int expectedSourceId = source.Id;
            int expectedTargetId = target.Id;
            int expectedTargetRequiredId = targetRequired.Id;
            using (var context = new AssociationsContext())
            {
                context.UpdateGraph(target, map => map.AssociatedEntity(c => c.RequiredAssociate).AssociatedCollection(c => c.Sources));
                context.SaveChanges();

                Assert.IsNotNull(context.RequiredAssociates.FirstOrDefault(p => p.Id == expectedTargetRequiredId));
                Assert.IsNotNull(context.RootEntities.FirstOrDefault(p => p.Id == expectedSourceId));

                var targetReloaded = context.RootEntities.Include("Sources").FirstOrDefault(c => c.Id == expectedTargetId);
                Assert.IsNotNull(targetReloaded);
                Assert.AreEqual(1, targetReloaded.Sources.Count);
                Assert.AreEqual(expectedSourceId, targetReloaded.Sources.First().Id);
            }
        }
    }

    public class AssociationsContext : DbContext
    {
        public IDbSet<RootEntity> RootEntities { get; set; }

        public IDbSet<RequiredAssociate> RequiredAssociates { get; set; }

        public AssociationsContext()
            : base("GraphDiff") { }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RootEntity>()
                    .HasOptional(c => c.Target)
                    .WithMany(c => c.Sources);
        }
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
}
