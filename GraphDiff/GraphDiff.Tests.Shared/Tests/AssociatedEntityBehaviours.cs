using System.Linq;
using System.Data.Entity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RefactorThis.GraphDiff.Tests.Models;

namespace RefactorThis.GraphDiff.Tests.Tests
{
    [TestClass]
    public class AssociatedEntityBehaviours : TestBase
    {
        [TestMethod]
        public void ShouldAddRelationIfPreviousValueWasNull()
        {
            var node1 = new TestNode { Title = "New Node" };
            var associated = new OneToOneAssociatedModel { Title = "Associated Node" };

            using (var context = new TestDbContext())
            {
                context.Nodes.Add(node1);
                context.OneToOneAssociatedModels.Add(associated);
                context.SaveChanges();
            } // Simulate detach

            node1.OneToOneAssociated = associated;

            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(node1, map => map
                    .AssociatedEntity(p => p.OneToOneAssociated));

                context.SaveChanges();
                var node2 = context.Nodes.Include(p => p.OneToOneAssociated).Single(p => p.Id == node1.Id);
                Assert.IsNotNull(node2);
                Assert.IsTrue(node2.OneToOneAssociated.OneParent == node2);
            }
        }

        [TestMethod]
        public void ShouldAddRelationIfPreviousValueWasNullWithCycle()
        {
            GroupedTestNode two;
            GroupedTestNode one;
            using (var context = new TestDbContext())
            {
                var group = new NodeGroup();
                context.NodeGroups.Add(group);
                context.SaveChanges();

                one = new GroupedTestNode { Group = group };
                context.Nodes.Add(one);
                context.SaveChanges();

                two = new GroupedTestNode { Group = group };
                context.Nodes.Add(two);
                context.SaveChanges();

                Assert.AreEqual(2, group.Members.Count);
            } // Simulate detach

            using (var context = new TestDbContext())
            {
                one.Two = two;

                // Setup mapping
                context.UpdateGraph(one, map => map.AssociatedEntity(o => o.Two));
                context.SaveChanges();

                var oneReloaded = context.Nodes.OfType<GroupedTestNode>().Include("Two").Single(n => n.Id == one.Id);
                Assert.IsNotNull(oneReloaded.Two);
                Assert.AreEqual(two.Id, oneReloaded.Two.Id);
            }
        }

        [TestMethod]
        public void ShouldRemoveAssociatedRelationIfNull()
        {
            var node1 = new TestNode { Title = "New Node", OneToOneAssociated = new OneToOneAssociatedModel { Title = "Associated Node" } };

            using (var context = new TestDbContext())
            {
                context.Nodes.Add(node1);
                context.SaveChanges();
            } // Simulate detach

            node1.OneToOneAssociated = null;

            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(node1, map => map
                    .AssociatedEntity(p => p.OneToOneAssociated));

                context.SaveChanges();
                var node2 = context.Nodes.Include(p => p.OneToOneAssociated).Single(p => p.Id == node1.Id);
                Assert.IsNotNull(node2);
                Assert.IsTrue(node2.OneToOneAssociated == null);
            }
        }

        [TestMethod]
        public void ShouldReplaceReferenceIfNewEntityIsNotPreviousEntity()
        {
            var node1 = new TestNode { 
                Title = "New Node",
                OneToOneAssociated = new OneToOneAssociatedModel { Title = "Associated Node" }
            };
            var otherModel = new OneToOneAssociatedModel { Title = "Hello" };

            using (var context = new TestDbContext())
            {
                context.Nodes.Add(node1);
                context.OneToOneAssociatedModels.Add(otherModel);
                context.SaveChanges();
            } // Simulate detach

            node1.OneToOneAssociated = otherModel;

            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(node1, map => map
                    .AssociatedEntity(p => p.OneToOneAssociated));

                context.SaveChanges();
                var node2 = context.Nodes.Include(p => p.OneToOneAssociated).Single(p => p.Id == node1.Id);
                Assert.IsNotNull(node2);
                Assert.IsTrue(node2.OneToOneAssociated.OneParent == node2);
                // should not delete it as it is associated and no cascade rule set.
                Assert.IsTrue(context.OneToOneAssociatedModels.Single(p => p.Id != otherModel.Id).OneParent == null);
            }
        }

        [TestMethod]
        public void ShouldNotUpdatePropertiesOfAnAssociatedEntity()
        {
            var node1 = new TestNode
            {
                Title = "New Node",
                OneToOneAssociated = new OneToOneAssociatedModel { Title = "Associated Node" }
            };

            using (var context = new TestDbContext())
            {
                context.Nodes.Add(node1);
                context.SaveChanges();
            } // Simulate detach


            node1.OneToOneAssociated.Title = "Updated Content";

            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(node1, map => map
                    .AssociatedEntity(p => p.OneToOneAssociated));

                context.SaveChanges();
                var node2 = context.Nodes.Include(p => p.OneToOneAssociated).Single(p => p.Id == node1.Id);
                Assert.IsNotNull(node2);
                Assert.IsTrue(node2.OneToOneAssociated.OneParent == node2);
                // should not delete it as it is associated and no cascade rule set.
                Assert.IsTrue(node2.OneToOneAssociated.Title == "Associated Node");
            }
        }
    }
}
