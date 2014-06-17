using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RefactorThis.GraphDiff.Aggregates;
using RefactorThis.GraphDiff.Tests.Models;
using RefactorThis.GraphDiff.Internal;
using RefactorThis.GraphDiff.Internal.Caching;
using System.Data.Entity;
using System.Collections.Generic;

namespace RefactorThis.GraphDiff.Tests.UnitTests
{
    [TestClass]
    public class AggregateConfigurationBahaviours : TestBase
    {
        [TestInitialize]
        public void Init()
        {
            AggregateConfiguration.Aggregates.ClearAll();
        }

        [TestMethod]
        public void ShouldUseAggregateConfigurationMappingForDefaultMappings()
        {
            AggregateConfiguration.Aggregates
                .Register<TestNode>(p => p.OwnedEntity(m => m.OneToOneOwned));

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
                context.UpdateGraph(node1);

                context.SaveChanges();
                var node2 = context.Nodes.Include(p => p.OneToOneOwned). Single(p => p.Id == node1.Id);
                Assert.IsNotNull(node2);
                Assert.IsTrue(node2.OneToOneOwned.OneParent == node2 && node2.OneToOneOwned.Title == "New Entity");
            }
        }

        [TestMethod]
        public void ShouldUseAggregateConfigurationMappingForMappingSchemeProvided()
        {
            // setup
            AggregateConfiguration.Aggregates
                .Register<TestNode>("NonDefaultScheme", map => map
                    .OwnedCollection(p => p.OneToManyOwned, with => with
                        .OwnedEntity(p => p.OneToManyOneToOneOwned)
                        .AssociatedEntity(p => p.OneToManyOneToOneAssociated)
                        .OwnedCollection(p => p.OneToManyOneToManyOwned)
                        .AssociatedCollection(p => p.OneToManyOneToManyAssociated)
                    )
                );

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
                context.UpdateGraph(node1, "NonDefaultScheme");
                context.SaveChanges();

                // assert
                node1 = context.Nodes.Single(p => p.Id == node1.Id);
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
    }
}
