using Microsoft.VisualStudio.TestTools.UnitTesting;
using RefactorThis.GraphDiff.Tests.Models;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;

namespace RefactorThis.GraphDiff.Tests.Tests
{
    [TestClass]
    public class AssociatedCollectionBehaviours : TestBase
    {
        [TestMethod]
        public void ShouldAddRelationToExistingAssociatedCollection()
        {
            var associated = new OneToManyAssociatedModel { Title = "Second One" };
            var node1 = new TestNode 
            { 
                Title = "New Node",
                OneToManyAssociated = new List<OneToManyAssociatedModel> 
                {
                    new OneToManyAssociatedModel { Title = "First One" }
                }
            };

            using (var context = new TestDbContext())
            {
                context.Nodes.Add(node1);
                context.OneToManyAssociatedModels.Add(associated);
                context.SaveChanges();
            } // Simulate detach

            node1.OneToManyAssociated.Add(associated);

            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(node1, map => map
                    .AssociatedCollection(p => p.OneToManyAssociated));

                context.SaveChanges();
                var node2 = context.Nodes.Include(p => p.OneToManyAssociated).Single(p => p.Id == node1.Id);
                Assert.IsNotNull(node2);
                Assert.IsTrue(node2.OneToManyAssociated.Count == 2);
            }
        }

        [TestMethod]
        public void ShouldRemoveRelationFromExistingAssociatedCollection()
        {
            var node1 = new TestNode
            {
                Title = "New Node",
                OneToManyAssociated = new List<OneToManyAssociatedModel> 
                {
                    new OneToManyAssociatedModel { Title = "First One" }
                }
            };

            using (var context = new TestDbContext())
            {
                context.Nodes.Add(node1);
                context.SaveChanges();
            } // Simulate detach

            node1.OneToManyAssociated.Remove(node1.OneToManyAssociated.First());

            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(node1, map => map
                    .AssociatedCollection(p => p.OneToManyAssociated));

                context.SaveChanges();
                var node2 = context.Nodes.Include(p => p.OneToManyAssociated).Single(p => p.Id == node1.Id);
                Assert.IsNotNull(node2);
                Assert.IsTrue(node2.OneToManyAssociated.Count == 0);
            }
        }

        [TestMethod]
        public void ShouldNotUpdateEntitesWithinAnAssociatedCollection()
        {
            var node1 = new TestNode
            {
                Title = "New Node",
                OneToManyAssociated = new List<OneToManyAssociatedModel> 
                {
                    new OneToManyAssociatedModel { Title = "First One" }
                }
            };

            using (var context = new TestDbContext())
            {
                context.Nodes.Add(node1);
                context.SaveChanges();
            } // Simulate detach

            node1.OneToManyAssociated.First().Title = "This should not overwrite value";

            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(node1, map => map
                    .AssociatedCollection(p => p.OneToManyAssociated));

                context.SaveChanges();
                var node2 = context.Nodes.Include(p => p.OneToManyAssociated).Single(p => p.Id == node1.Id);
                Assert.IsNotNull(node2);
                Assert.IsTrue(node2.OneToManyAssociated.Single().Title == "First One");
            }
        }

        [TestMethod]
        public void ShouldAddAssociateWithRequiredAssociate()
        {
            var targetRequired = new RequiredAssociate();
            var source = new RootEntity();
            var target = new RootEntity();

            using (var context = new TestDbContext())
            {
                context.RequiredAssociates.Add(targetRequired);

                context.RootEntities.Add(source);
                source.RequiredAssociate = targetRequired;

                context.RootEntities.Add(target);
                target.RequiredAssociate = targetRequired;

                context.SaveChanges();
            }

            target.Sources = new List<RootEntity> { source };

            int expectedSourceId = source.Id;
            int expectedTargetId = target.Id;
            int expectedTargetRequiredId = targetRequired.Id;
            using (var context = new TestDbContext())
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

        [TestMethod]
        public void ShouldAddAssociatedWithoutChangingRequiredAssociate()
        {
            var root = new RootEntity { RequiredAssociate = new RequiredAssociate(), Sources = new List<RootEntity>() };
            var requiredAssociate = new RequiredAssociate();
            var owned = new RootEntity { RequiredAssociate = requiredAssociate };

            using (var context = new TestDbContext())
            {
                context.RootEntities.Add(root);
                context.RootEntities.Add(owned);
                context.RequiredAssociates.Add(requiredAssociate);
                context.SaveChanges();
            } // Simulate detach

            var expectedAssociateId = requiredAssociate.Id;
            root.Sources.Add(owned);

            using (var context = new TestDbContext())
            {
                root = context.UpdateGraph(root, map => map.AssociatedCollection(r => r.Sources).AssociatedEntity(r => r.RequiredAssociate));
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
        public void ShouldAddTwoAssociatesWithSharedRequiredAssociate()
        {
            var root = new RootEntity { RequiredAssociate = new RequiredAssociate(), Sources = new List<RootEntity>() };
            var requiredAssociate = new RequiredAssociate();
            var associateOne = new RootEntity { RequiredAssociate = requiredAssociate };
            var associateTwo = new RootEntity { RequiredAssociate = requiredAssociate };

            using (var context = new TestDbContext())
            {
                context.RootEntities.Add(root);
                context.RootEntities.Add(associateOne);
                context.RootEntities.Add(associateTwo);
                context.RequiredAssociates.Add(requiredAssociate);
                context.SaveChanges();
            } // Simulate detach

            var expectedAssociateId = requiredAssociate.Id;
            root.Sources.Add(associateOne);
            root.Sources.Add(associateTwo);

            using (var context = new TestDbContext())
            {
                root = context.UpdateGraph(root, map => map.AssociatedCollection(r => r.Sources).AssociatedEntity(r => r.RequiredAssociate));
                context.SaveChanges();

                Assert.IsTrue(root.Sources.All(s => s.RequiredAssociate.Id == expectedAssociateId));

                var sourceIds = root.Sources.Select(s => s.Id).ToArray();
                var sourcesReloaded = context.RootEntities.Where(r => sourceIds.Contains(r.Id)).ToList();
                Assert.IsTrue(sourcesReloaded.All(s => s.RequiredAssociate != null && s.RequiredAssociate.Id == expectedAssociateId));
            }
        }

        [TestMethod]
        public void ShouldRemoveAssociateWithRequiredAssociate()
        {
            var targetRequired = new RequiredAssociate();
            var source = new RootEntity();
            var target = new RootEntity();

            using (var context = new TestDbContext())
            {
                context.RequiredAssociates.Add(targetRequired);

                context.RootEntities.Add(source);
                source.RequiredAssociate = targetRequired;

                context.RootEntities.Add(target);
                target.RequiredAssociate = targetRequired;
                target.Sources = new List<RootEntity> { source };

                context.SaveChanges();
            }

            target.Sources.Remove(source);

            int expectedSourceId = source.Id;
            int expectedTargetId = target.Id;
            int expectedTargetRequiredId = targetRequired.Id;
            using (var context = new TestDbContext())
            {
                context.UpdateGraph(target, map => map.AssociatedEntity(c => c.RequiredAssociate).AssociatedCollection(c => c.Sources));
                context.SaveChanges();

                Assert.IsNotNull(context.RequiredAssociates.FirstOrDefault(p => p.Id == expectedTargetRequiredId));
                Assert.IsNotNull(context.RootEntities.FirstOrDefault(p => p.Id == expectedSourceId));

                var targetReloaded = context.RootEntities.Include("Sources").FirstOrDefault(c => c.Id == expectedTargetId);
                Assert.IsNotNull(targetReloaded);
                Assert.AreEqual(0, targetReloaded.Sources.Count);
            }
        }

        [TestMethod]
        public void ShouldRemoveAssociateWithRequiredAssociateIfRequiredIsNotSet()
        {
            var targetRequired = new RequiredAssociate();
            var sourceRequired = new RequiredAssociate();
            var source = new RootEntity();
            var target = new RootEntity();

            using (var context = new TestDbContext())
            {
                context.RequiredAssociates.Add(targetRequired);
                context.RequiredAssociates.Add(sourceRequired);

                context.RootEntities.Add(source);
                source.RequiredAssociate = sourceRequired;

                context.RootEntities.Add(target);
                target.RequiredAssociate = targetRequired;
                target.Sources = new List<RootEntity> { source };

                context.SaveChanges();
            }

            target.Sources.Remove(source);

            int expectedSourceId = source.Id;
            int expectedTargetId = target.Id;
            int expectedTargetRequiredId = targetRequired.Id;
            using (var context = new TestDbContext())
            {
                context.UpdateGraph(target, map => map.AssociatedCollection(c => c.Sources).AssociatedEntity(c => c.RequiredAssociate));
                context.SaveChanges();

                Assert.IsNotNull(context.RequiredAssociates.FirstOrDefault(p => p.Id == expectedTargetRequiredId));
                Assert.IsNotNull(context.RootEntities.FirstOrDefault(p => p.Id == expectedSourceId));

                var targetReloaded = context.RootEntities.Include("Sources").FirstOrDefault(c => c.Id == expectedTargetId);
                Assert.IsNotNull(targetReloaded);
                Assert.AreEqual(0, targetReloaded.Sources.Count);
            }
        }

        //[TestMethod]
        //public void ShouldAddRelationToExistingAssociatedCollection_Attached()
        //{
        //    var associated = new OneToManyAssociatedModel { Title = "Second One" };
        //    var node1 = new TestNode
        //    {
        //        Title = "New Node",
        //        OneToManyAssociated = new List<OneToManyAssociatedModel> 
        //        {
        //            new OneToManyAssociatedModel { Title = "First One" }
        //        }
        //    };

        //    using (var context = new TestDbContext())
        //    {
        //        context.Nodes.Add(node1);
        //        context.OneToManyAssociatedModels.Add(associated);
        //        context.SaveChanges();

        //        node1.OneToManyAssociated.Add(associated);

        //        context.UpdateGraph(node1, map => map
        //            .AssociatedCollection(p => p.OneToManyAssociated));

        //        context.SaveChanges();
        //        var node2 = context.Nodes.Include(p => p.OneToManyAssociated).Single(p => p.Id == node1.Id);
        //        Assert.IsNotNull(node2);
        //        Assert.IsTrue(node2.OneToManyAssociated.Count == 2);
        //    }
        //}

        //[TestMethod]
        //public void ShouldRemoveRelationFromExistingAssociatedCollection_Attached()
        //{
        //    var node1 = new TestNode
        //    {
        //        Title = "New Node",
        //        OneToManyAssociated = new List<OneToManyAssociatedModel> 
        //        {
        //            new OneToManyAssociatedModel { Title = "First One" }
        //        }
        //    };

        //    using (var context = new TestDbContext())
        //    {
        //        context.Nodes.Add(node1);
        //        context.SaveChanges();

        //        node1.OneToManyAssociated.Remove(node1.OneToManyAssociated.First());

        //        // Setup mapping
        //        context.UpdateGraph(node1, map => map
        //            .AssociatedCollection(p => p.OneToManyAssociated));

        //        context.SaveChanges();
        //        var node2 = context.Nodes.Include(p => p.OneToManyAssociated).Single(p => p.Id == node1.Id);
        //        Assert.IsNotNull(node2);
        //        Assert.IsTrue(node2.OneToManyAssociated.Count == 0);
        //    }
        //}

        //[TestMethod]
        //public void ShouldNotUpdateEntitesWithinAnAssociatedCollection_Attached()
        //{
        //    var node1 = new TestNode
        //    {
        //        Title = "New Node",
        //        OneToManyAssociated = new List<OneToManyAssociatedModel> 
        //        {
        //            new OneToManyAssociatedModel { Title = "First One" }
        //        }
        //    };

        //    using (var context = new TestDbContext())
        //    {
        //        context.Nodes.Add(node1);
        //        context.SaveChanges();

        //        node1.OneToManyAssociated.First().Title = "This should not overwrite value";

        //        // Setup mapping
        //        context.UpdateGraph(node1, map => map
        //            .AssociatedCollection(p => p.OneToManyAssociated));

        //        context.SaveChanges();
        //        var node2 = context.Nodes.Include(p => p.OneToManyAssociated).Single(p => p.Id == node1.Id);
        //        Assert.IsNotNull(node2);
        //        Assert.IsTrue(node2.OneToManyAssociated.Single().Title == "First One");
        //    }
        //}
    }
}
