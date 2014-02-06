using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RefactorThis.GraphDiff.Tests.Tests
{
    public class GuidEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
    }

    public class GuidTestNode : GuidEntity
    {
        public GuidOneToOneOwned OneToOneOwned { get; set; }
    }

    public class GuidOneToOneOwned : GuidEntity
    {
        public GuidTestNode OneParent { get; set; }
    }

    [TestClass]
    public class GuidKeyBehaviors : TestBase
    {
        [TestMethod]
        public void ShouldSupportGuidKeys()
        {
            var model = new GuidTestNode();
            using (var context = new TestDbContext())
            {
                context.GuidKeyModels.Add(model);
                context.SaveChanges();

                // http://stackoverflow.com/questions/5270721/using-guid-as-pk-with-ef4-code-first
                Assert.IsTrue(Attribute.IsDefined(model.GetType().GetProperty("Id"), typeof(DatabaseGeneratedAttribute)));

                Assert.IsNotNull(model.Id);
                Assert.AreNotEqual(Guid.Empty, model.Id);
            } // simulate detach

            model.OneToOneOwned = new GuidOneToOneOwned();

            using (var context = new TestDbContext())
            {
                model = context.UpdateGraph(model, map => map.OwnedEntity(g => g.OneToOneOwned));
                context.SaveChanges();

                Assert.IsNotNull(model.OneToOneOwned);
                Assert.IsNotNull(model.OneToOneOwned.Id);
                Assert.AreNotEqual(Guid.Empty, model.OneToOneOwned.Id);
            }
        }
    }
}
