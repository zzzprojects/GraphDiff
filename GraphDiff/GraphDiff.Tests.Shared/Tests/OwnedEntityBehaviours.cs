using Microsoft.VisualStudio.TestTools.UnitTesting;
using RefactorThis.GraphDiff.Tests.Models;
using System.Linq;
using System.Data.Entity;

namespace RefactorThis.GraphDiff.Tests.Tests
{
    [TestClass]
    public class OwnedEntityBehaviours : TestBase
    {
        [TestMethod]
        public void ShouldAddNewEntityWhenAddedToParent()
        {
            var node1 = new TestNode { Title = "New Node" };

            using (var context = new TestDbContext())
            {
                context.Nodes.Add(node1);
                context.SaveChanges();
            } // Simulate detach

            node1.OneToOneOwned = new OneToOneOwnedModel { Title = "New Entity" };
            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(node1, map => map
                    .OwnedEntity(p => p.OneToOneOwned));

                context.SaveChanges();
                var node2 = context.Nodes.Include(p => p.OneToOneOwned).Single(p => p.Id == node1.Id);
                Assert.IsNotNull(node2);
                Assert.IsTrue(node2.OneToOneOwned.OneParent == node2 && node2.OneToOneOwned.Title == "New Entity");
            }
        }

        [TestMethod]
        public void ShouldAddNewEntityOfChildTypeWhenAddedToParent()
        {
            var root = new TestNodeWithBaseReference { Title = "New Node" };

            using (var context = new TestDbContext())
            {
                context.NodesWithReference.Add(root);
                context.SaveChanges();
            } // Simulate detach

            root.OneToOneOwnedBase = new TestChildNode { Title = "New Entity" };
            using (var context = new TestDbContext())
            {
                // Setup mapping
                var updatedRoot = context.UpdateGraph(root, map => map.OwnedEntity(p => p.OneToOneOwnedBase));
                context.SaveChanges();

                Assert.AreEqual(typeof(TestChildNode), updatedRoot.OneToOneOwnedBase.GetType());
            }
        }

        [TestMethod]
        public void ShouldUpdateValuesOfEntityWhenEntityAlreadyExists()
        {
            var node1 = new TestNode { Title = "New Node", OneToOneOwned = new OneToOneOwnedModel { Title = "New Entity" } };

            using (var context = new TestDbContext())
            {
                context.Nodes.Add(node1);
                context.SaveChanges();
            } // Simulate detach

            node1.OneToOneOwned.Title = "Newer Entity";
            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(node1, map => map
                    .OwnedEntity(p => p.OneToOneOwned));

                context.SaveChanges();
                var node2 = context.Nodes.Include(p => p.OneToOneOwned).Single(p => p.Id == node1.Id);
                Assert.IsNotNull(node2);
                Assert.IsTrue(node2.OneToOneOwned.OneParent == node2 && node2.OneToOneOwned.Title == "Newer Entity");
            }
        }

        [TestMethod]
        public void ShouldRemoveEntityIfRemovedFromParent()
        {
            var oneToOne = new OneToOneOwnedModel { Title = "New Entity" };
            var node1 = new TestNode { Title = "New Node", OneToOneOwned = oneToOne };

            using (var context = new TestDbContext())
            {
                context.Nodes.Add(node1);
                context.SaveChanges();
            } // Simulate detach

            node1.OneToOneOwned = null;
            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(node1, map => map
                    .OwnedEntity(p => p.OneToOneOwned));

                context.SaveChanges();
                var node2 = context.Nodes.Include(p => p.OneToOneOwned).Single(p => p.Id == node1.Id);
                Assert.IsNotNull(node2);
                Assert.IsNull(context.OneToOneOwnedModels.SingleOrDefault(p => p.Id == oneToOne.Id));
            }
        }
    }
}
