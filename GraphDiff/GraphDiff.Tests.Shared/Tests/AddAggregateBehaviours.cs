using System;
using System.Data.Entity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RefactorThis.GraphDiff.Tests.Models;
using System.Linq;
using System.Collections.Generic;

namespace RefactorThis.GraphDiff.Tests.Tests
{
    [TestClass]
    public class AddAggregateBehaviours : TestBase
    {
        [TestMethod]
        public void ShouldAddNewAggregateRootWithDuplicateEntityOnLevel1()
        {
            var guid = Guid.Parse("C1775DA8-61EC-47A1-ADF9-A50653820061");

            var node1 = new ModelRoot()
            {
                Id = Guid.NewGuid(),
                MyModelsLevel1 = new List<ModelLevel1>()
                {
                    new ModelLevel1() {Id = guid},
                    new ModelLevel1() {Id = guid}
                }
            };

            using (var context = new TestDbContext())
            {
                node1 = context.UpdateGraph(node1, map =>
                    map.OwnedCollection(p => p.MyModelsLevel1,
                        with => with.OwnedEntity(p => p.ModelLevel2)));

                context.SaveChanges();
            }

            using (var context = new TestDbContext())
            {
                var model = context.Set<ModelRoot>().Include(x => x.MyModelsLevel1).FirstOrDefault();
                Assert.IsTrue(model.MyModelsLevel1.All(x => x.Id == guid));
            }
        }

        [TestMethod]
        public void ShouldAddNewAggregateRootWithduplicateEntityOnLevel2()
        {
            // See TBD_79ad60a7-8091-4d0c-b5de-7373f3b8cedf
            //var guid = Guid.Parse("C1775DA8-61EC-47A1-ADF9-A50653820061");

            //var node1 = new ModelRoot()
            //{
            //    Id = Guid.NewGuid(),
            //    MyModelsLevel1 = new List<ModelLevel1>()
            //    {
            //        new ModelLevel1() {Id = Guid.NewGuid(), ModelLevel2 = new ModelLevel2() {Code = guid}},
            //        new ModelLevel1() {Id = Guid.NewGuid(), ModelLevel2 = new ModelLevel2() {Code = guid}}
            //    }
            //};

            //using (var context = new TestDbContext())
            //{
            //    node1 = context.UpdateGraph(node1, map =>
            //        map.OwnedCollection(p => p.MyModelsLevel1,
            //            with => with.OwnedEntity(p => p.ModelLevel2)));

            //    context.SaveChanges();
            //}

            //using (var context = new TestDbContext())
            //{
            //    var models = context.Set<ModelLevel1>().Include(x => x.ModelLevel2).ToList();
            //    Assert.IsTrue(models.All(x => x.ModelLevel2.Code == guid));
            //}
        }

        [TestMethod]
        public void ShouldAddNewAggregateRoot_Detached()
        {
            var associated = new OneToOneAssociatedModel { Title = "Associated" };
            var manyAssociated = new OneToManyAssociatedModel { Title = "Associated" };
            var node1 = new TestNode
            {
                Title = "New Node",
                OneToManyOwned = new List<OneToManyOwnedModel>
                {
                    new OneToManyOwnedModel { Title = "One" },
                    new OneToManyOwnedModel { Title = "Two" },
                    new OneToManyOwnedModel { Title = "Three" }
                },
                OneToManyAssociated = new List<OneToManyAssociatedModel>
                {
                    manyAssociated
                },
                OneToOneOwned = new OneToOneOwnedModel { Title = "OneToOne" },
                OneToOneAssociated = associated
            };

            using (var context = new TestDbContext())
            {
                context.OneToManyAssociatedModels.Add(manyAssociated);
                context.OneToOneAssociatedModels.Add(associated);
                context.SaveChanges();
            } // Simulate detach

            using (var context = new TestDbContext())
            {
                // Setup mapping
                node1 = context.UpdateGraph(node1, map => map
                    .OwnedEntity(p => p.OneToOneOwned)
                    .AssociatedEntity(p => p.OneToOneAssociated)
                    .OwnedCollection(p => p.OneToManyOwned, with => with.OwnedEntity(p => p.OneToManyOneToOneOwned))
                    .AssociatedCollection(p => p.OneToManyAssociated));

                context.SaveChanges();
                Assert.IsNotNull(context.Nodes.SingleOrDefault(p => p.Id == node1.Id));
            }
        }

        [TestMethod]
        public void ShouldAddNewAggregateRoot_Attached()
        {
            var associated = new OneToOneAssociatedModel { Title = "Associated" };
            var manyAssociated = new OneToManyAssociatedModel { Title = "Associated" };
            var node1 = new TestNode
            {
                Title = "New Node",
                OneToManyOwned = new List<OneToManyOwnedModel>
                {
                    new OneToManyOwnedModel { Title = "One" },
                    new OneToManyOwnedModel { Title = "Two" },
                    new OneToManyOwnedModel { Title = "Three" }
                },
                OneToManyAssociated = new List<OneToManyAssociatedModel>
                {
                    manyAssociated
                },
                OneToOneOwned = new OneToOneOwnedModel { Title = "OneToOne" },
                OneToOneAssociated = associated
            };

            using (var context = new TestDbContext())
            {
                context.OneToManyAssociatedModels.Add(manyAssociated);
                context.OneToOneAssociatedModels.Add(associated);

                // Setup mapping
                node1 = context.UpdateGraph(node1, map => map
                    .OwnedEntity(p => p.OneToOneOwned)
                    .AssociatedEntity(p => p.OneToOneAssociated)
                    .OwnedCollection(p => p.OneToManyOwned)
                    .AssociatedCollection(p => p.OneToManyAssociated));

                context.SaveChanges();
                Assert.IsNotNull(context.Nodes.SingleOrDefault(p => p.Id == node1.Id));
            }
        }

        [TestMethod]
        public void ShouldAddNewAggregateWithOwnedEntityAndOwnedCollection()
        {
            var node1 = new TestNode
            {
                Title = "New Node",
                OneToOneOwned = new OneToOneOwnedModel
                {
                    OneToOneOneToManyOwned = new[]
                    {
                        new OneToOneOneToManyOwnedModel {Title = "One"},
                        new OneToOneOneToManyOwnedModel {Title = "Two"},
                        new OneToOneOneToManyOwnedModel {Title = "Three"}
                    }
                }
            };

            using (var context = new TestDbContext())
            {
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
    }
}
