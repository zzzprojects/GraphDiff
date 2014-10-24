using System.Data.Entity;
using System.Linq;
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
            var node1 = new TestNode {Title = "New Node"};
            var associated = new OneToOneAssociatedModel {Title = "Associated Node"};

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
                TestNode node2 = context.Nodes.Include(p => p.OneToOneAssociated).Single(p => p.Id == node1.Id);
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

                one = new GroupedTestNode {Group = group};
                context.Nodes.Add(one);
                context.SaveChanges();

                two = new GroupedTestNode {Group = group};
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

                GroupedTestNode oneReloaded = context.Nodes.OfType<GroupedTestNode>().Include("Two").Single(n => n.Id == one.Id);
                Assert.IsNotNull(oneReloaded.Two);
                Assert.AreEqual(two.Id, oneReloaded.Two.Id);
            }
        }

        [TestMethod]
        public void ShouldRemoveAssociatedRelationIfNull()
        {
            var node1 = new TestNode {Title = "New Node", OneToOneAssociated = new OneToOneAssociatedModel {Title = "Associated Node"}};

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
                TestNode node2 = context.Nodes.Include(p => p.OneToOneAssociated).Single(p => p.Id == node1.Id);
                Assert.IsNotNull(node2);
                Assert.IsTrue(node2.OneToOneAssociated == null);
            }
        }

        [TestMethod]
        public void ShouldReplaceReferenceIfNewEntityIsNotPreviousEntity()
        {
            var node1 = new TestNode
            {
                Title = "New Node",
                OneToOneAssociated = new OneToOneAssociatedModel {Title = "Associated Node"}
            };
            var otherModel = new OneToOneAssociatedModel {Title = "Hello"};

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
                TestNode node2 = context.Nodes.Include(p => p.OneToOneAssociated).Single(p => p.Id == node1.Id);
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
                OneToOneAssociated = new OneToOneAssociatedModel {Title = "Associated Node"}
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
                TestNode node2 = context.Nodes.Include(p => p.OneToOneAssociated).Single(p => p.Id == node1.Id);
                Assert.IsNotNull(node2);
                Assert.IsTrue(node2.OneToOneAssociated.OneParent == node2);
                // should not delete it as it is associated and no cascade rule set.
                Assert.IsTrue(node2.OneToOneAssociated.Title == "Associated Node");
            }
        }

        [TestMethod]
        public void ShouldAddOneAssociateToMany()
        {
            var associate = new ManyToOneModel {Title = "TheOne"};
            var firstParent = new TestNode {ManyToOneAssociated = associate};

            using (var context = new TestDbContext())
            {
                context.ManyToOneModels.Add(associate);
                context.Nodes.Add(firstParent);
                context.SaveChanges();
            } // Simulate detach

            var secondParent = new TestNode {ManyToOneAssociated = associate};

            using (var context = new TestDbContext())
            {
                secondParent = context.UpdateGraph(secondParent, map => map.AssociatedEntity(e => e.ManyToOneAssociated));
                context.SaveChanges();

                Assert.IsNotNull(secondParent.ManyToOneAssociated);
                Assert.AreEqual(associate.Id, secondParent.ManyToOneAssociated.Id);

                Assert.AreEqual(1, context.ManyToOneModels.Count());
            }
        }

        [TestMethod]
        public void ShouldRemoveOneAssociateFromMany()
        {
            var associate = new ManyToOneModel {Title = "TheOne"};
            var firstParent = new TestNode {ManyToOneAssociated = associate};
            var secondParent = new TestNode {ManyToOneAssociated = associate};

            using (var context = new TestDbContext())
            {
                context.ManyToOneModels.Add(associate);
                context.Nodes.Add(firstParent);
                context.Nodes.Add(secondParent);
                context.SaveChanges();

                Assert.AreEqual(1, context.ManyToOneModels.Count());
            } // Simulate detach

            firstParent.ManyToOneAssociated = null;

            using (var context = new TestDbContext())
            {
                firstParent = context.UpdateGraph(firstParent, map => map.AssociatedEntity(e => e.ManyToOneAssociated));
                context.SaveChanges();

                firstParent = context.Nodes.Include(n => n.ManyToOneAssociated).Single(n => n.Id == firstParent.Id);

                Assert.IsNull(firstParent.ManyToOneAssociated);
                Assert.AreEqual(1, context.ManyToOneModels.Count());

                secondParent = context.Nodes.Include(n => n.ManyToOneAssociated).Single(n => n.Id == secondParent.Id);
                Assert.IsNotNull(secondParent.ManyToOneAssociated);
                Assert.AreEqual(associate.Id, secondParent.ManyToOneAssociated.Id);
            }

            secondParent.ManyToOneAssociated = null;

            using (var context = new TestDbContext())
            {
                secondParent = context.UpdateGraph(secondParent, map => map.AssociatedEntity(e => e.ManyToOneAssociated));
                context.SaveChanges();

                secondParent = context.Nodes.Include(n => n.ManyToOneAssociated).Single(n => n.Id == secondParent.Id);

                Assert.IsNull(secondParent.ManyToOneAssociated);
                Assert.AreEqual(1, context.ManyToOneModels.Count());
            }
        }

        [TestMethod]
        public void ShouldReplaceManyToOneAssociate()
        {
            var associate = new ManyToOneModel { Title = "TheOne" };
            var replacement = new ManyToOneModel { Title = "TheNewOne" };
            var firstParent = new TestNode { ManyToOneAssociated = associate };
            var secondParent = new TestNode { ManyToOneAssociated = associate };

            using (var context = new TestDbContext())
            {
                context.ManyToOneModels.Add(associate);
                context.ManyToOneModels.Add(replacement);
                context.Nodes.Add(firstParent);
                context.Nodes.Add(secondParent);
                context.SaveChanges();

                Assert.AreEqual(2, context.ManyToOneModels.Count());
            } // Simulate detach

            firstParent.ManyToOneAssociated = replacement;

            using (var context = new TestDbContext())
            {
                firstParent = context.UpdateGraph(firstParent, map => map.AssociatedEntity(e => e.ManyToOneAssociated));
                context.SaveChanges();

                firstParent = context.Nodes.Include(n => n.ManyToOneAssociated).Single(n => n.Id == firstParent.Id);

                Assert.IsNotNull(firstParent.ManyToOneAssociated);
                Assert.AreEqual(replacement.Id, firstParent.ManyToOneAssociated.Id);

                secondParent = context.Nodes.Include(n => n.ManyToOneAssociated).Single(n => n.Id == secondParent.Id);
                Assert.IsNotNull(secondParent.ManyToOneAssociated);
                Assert.AreEqual(associate.Id, secondParent.ManyToOneAssociated.Id);
            }
        }
    }
}