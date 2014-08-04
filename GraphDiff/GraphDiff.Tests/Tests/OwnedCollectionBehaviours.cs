using System.Collections.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RefactorThis.GraphDiff.Tests.Models;
using System.Linq;
using System.Data.Entity;
using System.Collections.Generic;
using System;

namespace RefactorThis.GraphDiff.Tests.Tests
{
    [TestClass]
    public class OwnedCollectionBehaviours : TestBase
    {
        [TestMethod]
        public void ShouldUpdateItemInOwnedCollection()
        {
            var node1 = new TestNode
            {
                Title = "New Node",
                OneToManyOwned = new List<OneToManyOwnedModel>
                {
                    new OneToManyOwnedModel { Title = "Hello" }
                }
            };

            using (var context = new TestDbContext())
            {
                context.Nodes.Add(node1);
                context.SaveChanges();
            } // Simulate detach

            node1.OneToManyOwned.First().Title = "What's up";
            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(node1, map => map
                    .OwnedCollection(p => p.OneToManyOwned));

                context.SaveChanges();
                var node2 = context.Nodes.Include(p => p.OneToManyOwned).Single(p => p.Id == node1.Id);
                Assert.IsNotNull(node2);
                var owned = node2.OneToManyOwned.First();
                Assert.IsTrue(owned.OneParent == node2 && owned.Title == "What's up");
            }
        }

        [TestMethod]
        public void ShouldUpdateItemInNestedOwnedCollection()
        {
            var node1 = new TestNode
            {
                Title = "New Node",
                OneToManyOwned = new List<OneToManyOwnedModel>
                {
                    new OneToManyOwnedModel
                    {
                        Title = "Hello",
                        OneToManyOneToManyOwned = new Collection<OneToManyOneToManyOwnedModel>
                        {
                            new OneToManyOneToManyOwnedModel {Title = "BeforeUpdate"}
                        }
                    }
                }
            };

            using (var context = new TestDbContext())
            {
                context.Nodes.Add(node1);
                context.SaveChanges();
            } // Simulate detach

            var oneToManyOneToManyOwned = node1.OneToManyOwned.Single().OneToManyOneToManyOwned.Single();
            var expectedId = oneToManyOneToManyOwned.Id;
            const string expectedTitle = "AfterUpdate";
            oneToManyOneToManyOwned.Title = expectedTitle;

            using (var context = new TestDbContext())
            {
                // Setup mapping
                node1 = context.UpdateGraph(node1, map => map.OwnedCollection(p => p.OneToManyOwned,
                                                                              with => with.OwnedCollection(p => p.OneToManyOneToManyOwned)));
                context.SaveChanges();

                Assert.AreEqual(1, node1.OneToManyOwned.Count);
                Assert.AreEqual(1, node1.OneToManyOwned.Single().OneToManyOneToManyOwned.Count);

                oneToManyOneToManyOwned = node1.OneToManyOwned.Single().OneToManyOneToManyOwned.Single();
                Assert.AreEqual(expectedId, oneToManyOneToManyOwned.Id);
                Assert.AreEqual(expectedTitle, oneToManyOneToManyOwned.Title);

                var node1Reloaded = context.Nodes
                        .Include("OneToManyOwned.OneToManyOneToManyOwned")
                        .Single(n => n.Id == node1.Id);

                Assert.AreEqual(1, node1Reloaded.OneToManyOwned.Count);
                Assert.AreEqual(1, node1Reloaded.OneToManyOwned.Single().OneToManyOneToManyOwned.Count);

                oneToManyOneToManyOwned = node1Reloaded.OneToManyOwned.Single().OneToManyOneToManyOwned.Single();
                Assert.AreEqual(expectedId, oneToManyOneToManyOwned.Id);
                Assert.AreEqual(expectedTitle, oneToManyOneToManyOwned.Title);
            }
        }

        [TestMethod]
        public void ShouldAddNewItemInOwnedCollection()
        {
            var node1 = new TestNode
            {
                Title = "New Node",
                OneToManyOwned = new List<OneToManyOwnedModel>
                {
                    new OneToManyOwnedModel { Title = "Hello" }
                }
            };

            using (var context = new TestDbContext())
            {
                context.Nodes.Add(node1);
                context.SaveChanges();
            } // Simulate detach

            var newModel = new OneToManyOwnedModel { Title = "Hi" };
            node1.OneToManyOwned.Add(newModel);

            using (var context = new TestDbContext())
            {
                node1 = context.UpdateGraph(node1, map => map.OwnedCollection(p => p.OneToManyOwned));
                context.SaveChanges();

                var node2 = context.Nodes.Include(p => p.OneToManyOwned).Single(p => p.Id == node1.Id);
                Assert.IsNotNull(node2);
                Assert.AreEqual(2, node2.OneToManyOwned.Count);

                var ownedId = node1.OneToManyOwned.Skip(1).Select(o => o.Id).Single();
                var owned = context.OneToManyOwnedModels.Single(p => p.Id == ownedId);
                Assert.IsTrue(owned.OneParent == node2 && owned.Title == "Hi");
            }
        }

        [TestMethod]
        public void ShouldAddNewItemInOwnedCollectionWithoutChangingRequiredAssociate()
        {
            var root = new RootEntity { RequiredAssociate = new RequiredAssociate(), Sources = new List<RootEntity>() };
            var requiredAssociate = new RequiredAssociate();
            using (var context = new TestDbContext())
            {
                context.RootEntities.Add(root);
                context.RequiredAssociates.Add(requiredAssociate);
                context.SaveChanges();
            } // Simulate detach

            var expectedAssociateId = requiredAssociate.Id;
            var owned = new RootEntity { RequiredAssociate = requiredAssociate };
            root.Sources.Add(owned);

            using (var context = new TestDbContext())
            {
                root = context.UpdateGraph(root, map => map.OwnedCollection(r => r.Sources, with => with.AssociatedEntity(s => s.RequiredAssociate)));
                context.SaveChanges();

                var ownedAfterSave = root.Sources.FirstOrDefault();
                Assert.IsNotNull(ownedAfterSave);
                Assert.IsNotNull(ownedAfterSave.RequiredAssociate);
                Assert.AreEqual(expectedAssociateId, ownedAfterSave.RequiredAssociate.Id);

                var ownedReloaded = context.RootEntities.Single(r => r.Id == ownedAfterSave.Id);
                Assert.IsNotNull(ownedReloaded.RequiredAssociate);
                Assert.AreEqual(expectedAssociateId, ownedReloaded.RequiredAssociate.Id);
            }
        }

        [TestMethod]
        public void ShouldAddTwoNewOwnedItemsWithSharedRequiredAssociate()
        {
            var root = new RootEntity { RequiredAssociate = new RequiredAssociate(), Sources = new List<RootEntity>() };
            var requiredAssociate = new RequiredAssociate();
            using (var context = new TestDbContext())
            {
                context.RootEntities.Add(root);
                context.RequiredAssociates.Add(requiredAssociate);
                context.SaveChanges();
            } // Simulate detach

            var expectedAssociateId = requiredAssociate.Id;
            var ownedOne = new RootEntity { RequiredAssociate = requiredAssociate };
            root.Sources.Add(ownedOne);
            var ownedTwo = new RootEntity { RequiredAssociate = requiredAssociate };
            root.Sources.Add(ownedTwo);

            using (var context = new TestDbContext())
            {
                root = context.UpdateGraph(root, map => map.OwnedCollection(r => r.Sources, with => with.AssociatedEntity(s => s.RequiredAssociate)));
                context.SaveChanges();

                Assert.IsTrue(root.Sources.All(s => s.RequiredAssociate.Id == expectedAssociateId));

                var sourceIds = root.Sources.Select(s => s.Id).ToArray();
                var sourcesReloaded = context.RootEntities.Where(r => sourceIds.Contains(r.Id)).ToList();
                Assert.IsTrue(sourcesReloaded.All(s => s.RequiredAssociate != null && s.RequiredAssociate.Id == expectedAssociateId));
            }
        }

        [TestMethod]
        public void ShouldNotChangeRequiredAssociateEvenIfItIsUsedTwice()
        {
            var requiredAssociate = new RequiredAssociate();
            var root = new RootEntity { RequiredAssociate = requiredAssociate, Sources = new List<RootEntity>() };
            using (var context = new TestDbContext())
            {
                context.RootEntities.Add(root);
                context.RequiredAssociates.Add(requiredAssociate);
                context.SaveChanges();
            } // Simulate detach

            var expectedAssociateId = requiredAssociate.Id;
            var owned = new RootEntity { RequiredAssociate = requiredAssociate };
            root.Sources.Add(owned);

            using (var context = new TestDbContext())
            {
                root = context.UpdateGraph(root, map => map.OwnedCollection(r => r.Sources, with => with.AssociatedEntity(s => s.RequiredAssociate)));
                context.SaveChanges();

                var ownedAfterSave = root.Sources.FirstOrDefault();
                Assert.IsNotNull(ownedAfterSave);
                Assert.IsNotNull(ownedAfterSave.RequiredAssociate);
                Assert.AreEqual(expectedAssociateId, ownedAfterSave.RequiredAssociate.Id);

                var ownedReloaded = context.RootEntities.Single(r => r.Id == ownedAfterSave.Id);
                Assert.IsNotNull(ownedReloaded.RequiredAssociate);
                Assert.AreEqual(expectedAssociateId, ownedReloaded.RequiredAssociate.Id);
            }
        }

        [TestMethod]
        public void ShouldRemoveItemsInOwnedCollection()
        {
            var node1 = new TestNode
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
                context.Nodes.Add(node1);
                context.SaveChanges();
            } // Simulate detach

            node1.OneToManyOwned.Remove(node1.OneToManyOwned.First());
            node1.OneToManyOwned.Remove(node1.OneToManyOwned.First());
            node1.OneToManyOwned.Remove(node1.OneToManyOwned.First());
            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(node1, map => map
                    .OwnedCollection(p => p.OneToManyOwned));

                context.SaveChanges();
                var node2 = context.Nodes.Include(p => p.OneToManyOwned).Single(p => p.Id == node1.Id);
                Assert.IsNotNull(node2);
                Assert.IsTrue(node2.OneToManyOwned.Count == 0);
            }
        }

        [TestMethod]
        public void ShouldRemoveItemsInOwnedCollectionWhenSetToNull()
        {
            var node1 = new TestNode
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
                context.Nodes.Add(node1);
                context.SaveChanges();
            } // Simulate detach

            node1.OneToManyOwned = null;
            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(node1, map => map
                    .OwnedCollection(p => p.OneToManyOwned));

                context.SaveChanges();
                var node2 = context.Nodes.Include(p => p.OneToManyOwned).Single(p => p.Id == node1.Id);
                Assert.IsNotNull(node2);
                Assert.IsTrue(node2.OneToManyOwned.Count == 0);
            }
        }

        [TestMethod]
        public void ShouldMergeTwoCollectionsAndDecideOnUpdatesDeletesAndAdds()
        {
            var node1 = new TestNode
            {
                Title = "New Node",
                OneToManyOwned = new List<OneToManyOwnedModel>
                {
                    new OneToManyOwnedModel { Title = "This" },
                    new OneToManyOwnedModel { Title = "Is" },
                    new OneToManyOwnedModel { Title = "A" },
                    new OneToManyOwnedModel { Title = "Test" }
                }
            };

            using (var context = new TestDbContext())
            {
                context.Nodes.Add(node1);
                context.SaveChanges();
            } // Simulate detach

            node1.OneToManyOwned.Remove(node1.OneToManyOwned.First());
            node1.OneToManyOwned.First().Title = "Hello";
            node1.OneToManyOwned.Add(new OneToManyOwnedModel { Title = "Finish" });
            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(node1, map => map
                    .OwnedCollection(p => p.OneToManyOwned));

                context.SaveChanges();
                var node2 = context.Nodes.Include(p => p.OneToManyOwned).Single(p => p.Id == node1.Id);
                Assert.IsNotNull(node2);
                var list = node2.OneToManyOwned.ToList();
                Assert.IsTrue(list[0].Title == "Hello");
                Assert.IsTrue(list[1].Title == "A");
                Assert.IsTrue(list[2].Title == "Test");
                Assert.IsTrue(list[3].Title == "Finish");
            }
        }

        [TestMethod]
        public void ShouldUpdateItemInOwnedCollectionWithCustomKey()
        {
            var node1 = new TestNode
            {
                Title = "New Node",
                OneToManyOwned = new List<OneToManyOwnedModel>
                {
                    new OneToManyOwnedModel { Title = "Hello", UniqueId = new Guid("DA6B78FF-BB7F-4FA1-8659-F64AC6457D14") }
                }
            };

            int originalOwnedId;
            using (var context = new TestDbContext())
            {
                context.Nodes.Add(node1);
                context.SaveChanges();
                originalOwnedId = node1.OneToManyOwned.First().Id;
            } // Simulate detach

            node1.OneToManyOwned.First().Title = "What's up";
            node1.OneToManyOwned.First().Id = 0; //We will try to update on Guid

            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(node1, map => map
                    .OwnedCollection(p => p.OneToManyOwned),
                    keysConfiguration: new KeysConfiguration()
                    .ForEntity<OneToManyOwnedModel>(e => e.UniqueId));

                context.SaveChanges();
                var node2 = context.Nodes.Include(p => p.OneToManyOwned).Single(p => p.Id == node1.Id);
                Assert.IsNotNull(node2);
                var owned = node2.OneToManyOwned.First();
                Assert.IsTrue(owned.OneParent == node2 && owned.Title == "What's up" && owned.Id == originalOwnedId);
            }
        }
    }
}
