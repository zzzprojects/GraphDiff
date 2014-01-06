using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RefactorThis.GraphDiff.Tests.Models;

namespace RefactorThis.GraphDiff.Tests.Tests
{
    [TestClass]
    public class ThirdTierBehaviours : TestBase
    {
        [TestMethod]
        public void ShouldSupportNullValuesInTree()
        {
            var node1 = new TestNode
            {
                Title = "New Node",
                OneToOneOwned = null
            };

            using (var context = new TestDbContext())
            {
                context.Nodes.Add(node1);
                context.SaveChanges();
            } // Simulate detach

            using (var context = new TestDbContext())
            {
                // Setup mapping
                node1 = context.UpdateGraph(node1, map => map
                    .OwnedEntity(p => p.OneToOneOwned, with =>
                        with.OwnedEntity(p => p.OneToOneOneToOneOwned)));

                context.SaveChanges();
                context.Entry(node1).Reload();
                Assert.IsTrue(node1.OneToOneOwned == null);
            }
        }

        [TestMethod]
        public void ShouldSupportOwnedEntityWithNestedModels()
        {
            // setup
            var oneToOneAssociated = new OneToOneOneToOneAssociatedModel { Title = "Associated Update" };
            var oneToManyAssociated = new OneToOneOneToManyAssociatedModel { Title = "Many Associated Update" };
            var node1 = new TestNode
            {
                Title = "New Node",
                OneToOneOwned = new OneToOneOwnedModel
                {
                    OneToOneOneToOneAssociated = new OneToOneOneToOneAssociatedModel { Title = "Hello" },
                    OneToOneOneToOneOwned = new OneToOneOneToOneOwnedModel { Title = "Hello" },
                    OneToOneOneToManyAssociated = new List<OneToOneOneToManyAssociatedModel>
                    {
                        new OneToOneOneToManyAssociatedModel { Title = "Hello" },
                        new OneToOneOneToManyAssociatedModel { Title = "Hello" }
                    },
                    OneToOneOneToManyOwned = new List<OneToOneOneToManyOwnedModel>
                    {
                        new OneToOneOneToManyOwnedModel { Title = "Hello" },
                        new OneToOneOneToManyOwnedModel { Title = "Hello" },
                        new OneToOneOneToManyOwnedModel { Title = "Hello" }
                    }
                }
            };

            using (var context = new TestDbContext())
            {
                context.Set<OneToOneOneToOneAssociatedModel>().Add(oneToOneAssociated);
                context.Set<OneToOneOneToManyAssociatedModel>().Add(oneToManyAssociated);
                context.Nodes.Add(node1);
                context.SaveChanges();
            } // Simulate detach

            // change
            node1.OneToOneOwned.OneToOneOneToOneOwned.Title = "Updated";
            node1.OneToOneOwned.OneToOneOneToOneAssociated = oneToOneAssociated;

            var owned = node1.OneToOneOwned.OneToOneOneToManyOwned;
            owned.Remove(owned.First());
            owned.First().Title = "Updated";
            owned.Skip(1).First().Title = "Updated 2";
            owned.Add(new OneToOneOneToManyOwnedModel { Title = "A new one" });

            var associated = node1.OneToOneOwned.OneToOneOneToManyAssociated;
            associated.Remove(associated.First());
            associated.Add(oneToManyAssociated);

            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(node1, map => map
                    .OwnedEntity(p => p.OneToOneOwned, with => with
                        .OwnedEntity(p => p.OneToOneOneToOneOwned)
                        .AssociatedEntity(p => p.OneToOneOneToOneAssociated)
                        .OwnedCollection(p => p.OneToOneOneToManyOwned)
                        .AssociatedCollection(p => p.OneToOneOneToManyAssociated)));

                context.SaveChanges();

                var updated = context.Set<OneToOneOwnedModel>().Single();
                Assert.IsNotNull(updated);
                Assert.IsTrue(updated.OneToOneOneToOneOwned.Title == "Updated");
                Assert.IsTrue(updated.OneToOneOneToOneAssociated.Title == "Associated Update");

                var ownershipList = updated.OneToOneOneToManyOwned.ToList();
                Assert.IsTrue(ownershipList.Count == 3);
                Assert.IsTrue(ownershipList[0].Title == "Updated");
                Assert.IsTrue(ownershipList[1].Title == "Updated 2");
                Assert.IsTrue(ownershipList[2].Title == "A new one");

                var associatedList = updated.OneToOneOneToManyAssociated.ToList();
                Assert.IsTrue(associatedList.Count == 2);
                Assert.IsTrue(associatedList[0].Title == "Hello");
                Assert.IsTrue(associatedList[1].Title == "Many Associated Update");
            }
        }

        [TestMethod]
        public void ShouldSupportOwnedCollectionWithNestedModels()
        {
            // setup
            var oneToOneAssociations = new List<OneToManyOneToOneAssociatedModel>();
            var oneToManyAssociations = new List<OneToManyOneToManyAssociatedModel>();

            var node1 = new TestNode
            {
                Title = "New Node",
                OneToManyOwned = new List<OneToManyOwnedModel>()
            };

            for (var i = 0; i < 3; i++)
            {
                node1.OneToManyOwned.Add(new OneToManyOwnedModel
                {
                    OneToManyOneToOneAssociated = new OneToManyOneToOneAssociatedModel { Title = "Hello" },
                    OneToManyOneToOneOwned = new OneToManyOneToOneOwnedModel { Title = "Hello" },
                    OneToManyOneToManyAssociated = new List<OneToManyOneToManyAssociatedModel>
                        {
                            new OneToManyOneToManyAssociatedModel { Title = "Hello" },
                            new OneToManyOneToManyAssociatedModel { Title = "Hello" }
                        },
                    OneToManyOneToManyOwned = new List<OneToManyOneToManyOwnedModel>
                        {
                            new OneToManyOneToManyOwnedModel { Title = "Hello" },
                            new OneToManyOneToManyOwnedModel { Title = "Hello" },
                            new OneToManyOneToManyOwnedModel { Title = "Hello" }
                        }
                });

                oneToOneAssociations.Add(new OneToManyOneToOneAssociatedModel { Title = "Associated Update" });
                oneToManyAssociations.Add(new OneToManyOneToManyAssociatedModel { Title = "Many Associated Update" + i });
            }

            using (var context = new TestDbContext())
            {
                foreach (var association in oneToOneAssociations)
                {
                    context.Set<OneToManyOneToOneAssociatedModel>().Add(association);
                }
                foreach (var association in oneToManyAssociations)
                {
                    context.Set<OneToManyOneToManyAssociatedModel>().Add(association);
                }
                context.Nodes.Add(node1);
                context.SaveChanges();
            } // Simulate detach

            // change
            var collection = node1.OneToManyOwned;
            for (var i = 0; i < collection.Count; i++)
            {
                var element = collection.Skip(i).First();
                element.OneToManyOneToOneOwned.Title = "Updated" + i;
                element.OneToManyOneToOneAssociated = oneToOneAssociations[i];

                var owned = element.OneToManyOneToManyOwned;
                owned.Remove(owned.First());
                owned.First().Title = "Updated" + i;
                owned.Skip(1).First().Title = "Updated 2" + i;
                owned.Add(new OneToManyOneToManyOwnedModel { Title = "A new one" + i });

                var associated = element.OneToManyOneToManyAssociated;
                associated.Remove(associated.First());
                associated.Add(oneToManyAssociations[i]);
            }

            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(node1, map => map
                    .OwnedCollection(p => p.OneToManyOwned, with => with
                        .OwnedEntity(p => p.OneToManyOneToOneOwned)
                        .AssociatedEntity(p => p.OneToManyOneToOneAssociated)
                        .OwnedCollection(p => p.OneToManyOneToManyOwned)
                        .AssociatedCollection(p => p.OneToManyOneToManyAssociated)));

                context.SaveChanges();

                // assert
                node1 = context.Nodes.Single();
                var list = node1.OneToManyOwned.ToList();
                for (var i = 0; i < collection.Count; i++)
                {
                    var element = list[i];
                    Assert.IsNotNull(element);
                    Assert.IsTrue(element.OneToManyOneToOneOwned.Title == "Updated" + i);
                    Assert.IsTrue(element.OneToManyOneToOneAssociated.Title == "Associated Update");

                    var ownershipList = element.OneToManyOneToManyOwned.ToList();
                    Assert.IsTrue(ownershipList.Count == 3);
                    Assert.IsTrue(ownershipList[0].Title == "Updated" + i);
                    Assert.IsTrue(ownershipList[1].Title == "Updated 2" + i);
                    Assert.IsTrue(ownershipList[2].Title == "A new one" + i);

                    var associatedList = element.OneToManyOneToManyAssociated.ToList();
                    Assert.IsTrue(associatedList.Count == 2);
                    Assert.IsTrue(associatedList[0].Title == "Hello");
                    Assert.IsTrue(associatedList[1].Title == "Many Associated Update" + i);
                }
            }
        }

        [TestMethod]
        public void ShouldUpdateAggregateWithOwnedEntityAndOwnedCollection()
        {
            var node1 = new TestNode
            {
                Title = "New Node"
            };

            using (var context = new TestDbContext())
            {
                context.Nodes.Add(node1);
                context.SaveChanges();
            }

            using (var context = new TestDbContext())
            {
                node1.Title = "Newly Updated";
                node1.OneToOneOwned = new OneToOneOwnedModel
                {
                    OneToOneOneToManyOwned = new[]
                    {
                        new OneToOneOneToManyOwnedModel {Title = "One"},
                        new OneToOneOneToManyOwnedModel {Title = "Two"},
                        new OneToOneOneToManyOwnedModel {Title = "Three"}
                    }
                };

                node1 = context.UpdateGraph(node1, map => map.OwnedEntity(p => p.OneToOneOwned, with =>
                    with.OwnedCollection(p => p.OneToOneOneToManyOwned)));
                context.SaveChanges();
            }

            using (var context = new TestDbContext())
            {
                var reload = context.Nodes
                    .Include("OneToOneOwned.OneToOneOneToManyOwned")
                    .SingleOrDefault(p => p.Id == node1.Id);

                Assert.IsNotNull(reload);
                Assert.AreEqual(node1.Title, reload.Title);
                Assert.IsNotNull(reload.OneToOneOwned);
                Assert.AreEqual(node1.OneToOneOwned.Id, reload.OneToOneOwned.Id);

                Assert.IsNotNull(reload.OneToOneOwned.OneToOneOneToManyOwned);
                Assert.AreEqual(3, reload.OneToOneOwned.OneToOneOneToManyOwned.Count);
                Assert.AreEqual(node1.OneToOneOwned.OneToOneOneToManyOwned.First().Id, node1.OneToOneOwned.OneToOneOneToManyOwned.First().Id);

            }
        }
        
        //// TODO
        //// need to add test for OwnedEntityGraphNode line 23
        //// shoudl remove old value.
    }
}
