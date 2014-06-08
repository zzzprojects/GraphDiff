using System.Collections.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RefactorThis.GraphDiff.Tests.Models;
using System.Linq;
using System.Data.Entity;
using System.Collections.Generic;

namespace RefactorThis.GraphDiff.Tests.Tests
{
    [TestClass]
    public class AttributeMappingBehaviours : TestBase
    {
        private AttributeTest _graph;

        [TestInitialize]
        public void Initialize()
        {
            _graph = new AttributeTest
            {
                Title = "Hello",
                OneToManyAssociated = new List<AttributeTestOneToManyAssociated>
                {
                    new AttributeTestOneToManyAssociated
                    {
                        Title = "Hello"
                    }
                },
                OneToManyOwned = new List<AttributeTestOneToManyOwned>
                {
                    new AttributeTestOneToManyOwned 
                    { 
                        Title = "Hello",
                        AttributeTestOneToManyToOneAssociated = new AttributeTestOneToManyToOneAssociated
                        {
                            Title = "Hello"
                        },
                        AttributeTestOneToManyToOneOwned = new AttributeTestOneToManyToOneOwned
                        {
                            Title = "Hello"
                        }
                    }
                }
            };
        }

        [TestMethod]
        public void ShouldUpdateItemsInCollections()
        {
            using (var context = new TestDbContext())
            {
                context.Attributes.Add(_graph);
                context.SaveChanges();
            } // Simulate detach

            // update it
            _graph.Title = "Hello1";
            var owned = _graph.OneToManyOwned.First();
            var associated = _graph.OneToManyAssociated.First();
            owned.Title = "Hello1";
            owned.AttributeTestOneToManyToOneOwned.Title = "Hello1";
            owned.AttributeTestOneToManyToOneAssociated.Title = "Hello1";
            associated.Title = "Hello1";

            using (var context = new TestDbContext())
            {
                context.UpdateGraph(_graph);
                context.SaveChanges();
            }

            using (var context = new TestDbContext())
            {
                var graph = context
                    .AggregateQuery<AttributeTest>()
                    .FirstOrDefault();

                Assert.IsNotNull(graph);
                Assert.IsTrue(graph.Title == "Hello1");

                var first = graph.OneToManyOwned.First();
                var associated1 = graph.OneToManyAssociated.First();

                Assert.IsTrue(first.Title == "Hello1");
                Assert.IsTrue(first.AttributeTestOneToManyToOneOwned.Title == "Hello1");
                Assert.IsTrue(first.AttributeTestOneToManyToOneAssociated.Title == "Hello");
                Assert.IsTrue(associated1.Title == "Hello");
            }
        }
    }
}
