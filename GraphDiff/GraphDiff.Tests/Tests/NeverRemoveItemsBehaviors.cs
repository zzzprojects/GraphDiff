using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RefactorThis.GraphDiff.Tests.Models;

namespace RefactorThis.GraphDiff.Tests.Tests
{
    [TestClass]
    public class NeverRemoveItemsBehaviors
    {
        [TestMethod]
        public void ShouldNeverRemoveFromAssociatedCollection()
        {
            var node = new TestNode
            {
                Title = "New Node",
                OneToManyAssociated = new List<OneToManyAssociatedModel>
                {
                    new OneToManyAssociatedModel { Title = "First One" }
                }
            };

            using (var context = new TestDbContext())
            {
                context.Nodes.Add(node);
                context.SaveChanges();
            } // Simulate detach

            node.OneToManyAssociated.Remove(node.OneToManyAssociated.Single());

            GraphDiffConfiguration.NeverRemoveFromCollections = true;

            using (var context = new TestDbContext())
            {
                node = context.UpdateGraph(node, map => map.AssociatedCollection(p => p.OneToManyAssociated));
                context.SaveChanges();

                Assert.AreEqual(1, node.OneToManyAssociated.Count);

                var nodeReloaded = context.Nodes.Include(p => p.OneToManyAssociated).Single(p => p.Id == node.Id);
                Assert.IsNotNull(nodeReloaded);
                Assert.AreEqual(1, nodeReloaded.OneToManyAssociated.Count);
            }
        }

        [TestMethod]
        public void ShouldNeverRemoveFromOwnedCollection()
        {
            var node = new TestNode
            {
                Title = "New Node",
                OneToManyOwned = new List<OneToManyOwnedModel>
                {
                    new OneToManyOwnedModel { Title = "Hello" },
                    new OneToManyOwnedModel { Title = "Hello2" },
                    new OneToManyOwnedModel { Title = "Hello3" }
                }
            };

            using (var context = new TestDbContext())
            {
                context.Nodes.Add(node);
                context.SaveChanges();
            } // Simulate detach

            node.OneToManyOwned.Remove(node.OneToManyOwned.First());
            node.OneToManyOwned.Remove(node.OneToManyOwned.First());
            node.OneToManyOwned.Remove(node.OneToManyOwned.First());

            GraphDiffConfiguration.NeverRemoveFromCollections = true;

            using (var context = new TestDbContext())
            {
                node = context.UpdateGraph(node, map => map.OwnedCollection(p => p.OneToManyOwned));
                context.SaveChanges();

                Assert.AreEqual(3, node.OneToManyOwned.Count);

                var reloadedNode = context.Nodes.Include(p => p.OneToManyOwned).Single(p => p.Id == node.Id);
                Assert.IsNotNull(reloadedNode);
                Assert.AreEqual(3, reloadedNode.OneToManyOwned.Count);
            }
        }
    }
}
