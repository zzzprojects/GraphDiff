using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RefactorThis.GraphDiff.Tests.Models;

namespace RefactorThis.GraphDiff.Tests.Tests
{
    [TestClass]
    public class AttributeMappingBehaviours : TestBase
    {
        private AttributeTest _graph;
        private SharedModelAttributeTest _sharedModelTestGraph;

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

            _sharedModelTestGraph = new SharedModelAttributeTest
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
                        },
                        SharedModelTesting = new AttributeTestOneToManyToOneOwned
                        {
                            Title = "Hellox"
                        }
                    }
                }
            };
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void ShouldThrowIfCircularGraph()
        {
            using (var context = new TestDbContext())
            {
                context.UpdateGraph(new CircularAttributeTest());
            }
        }

        [TestMethod]
        public void ShouldOnlyReadAttributesOfTheCurrentAggregate()
        {
            using (var context = new TestDbContext())
            {
                context.SharedModelAttributes.Add(_sharedModelTestGraph);
                context.SaveChanges();
            } // Simulate detach

            // update it
            _sharedModelTestGraph.Title = "Hello1";
            var owned = _sharedModelTestGraph.OneToManyOwned.First();
            var associated = _sharedModelTestGraph.OneToManyAssociated.First();
            owned.Title = "Hello1";
            owned.AttributeTestOneToManyToOneOwned.Title = "Hello1";
            owned.AttributeTestOneToManyToOneAssociated.Title = "Hello1";
            associated.Title = "Hello1";
            owned.SharedModelTesting.Title = "Hellox2x2";

            using (var context = new TestDbContext())
            {
                context.UpdateGraph(_sharedModelTestGraph);
                context.SaveChanges();
            }

            using (var context = new TestDbContext())
            {
                var graph = context.SharedModelAttributes
                    .Include(p => p.OneToManyAssociated)
                    .Include(p => p.OneToManyOwned.Select(m => m.SharedModelTesting))
                    .Include(p => p.OneToManyOwned.Select(m => m.AttributeTestOneToManyToOneAssociated))
                    .Include(p => p.OneToManyOwned.Select(m => m.AttributeTestOneToManyToOneOwned))
                    .First();

                Assert.IsNotNull(graph);
                Assert.IsTrue(graph.Title == "Hello1");

                var first = graph.OneToManyOwned.First();
                var associated1 = graph.OneToManyAssociated.First();

                Assert.IsTrue(first.Title == "Hello1");
                Assert.IsTrue(first.AttributeTestOneToManyToOneOwned.Title == "Hello");
                Assert.IsTrue(first.AttributeTestOneToManyToOneAssociated.Title == "Hello");
                Assert.IsTrue(first.SharedModelTesting.Title == "Hellox2x2");
                Assert.IsTrue(associated1.Title == "Hello");
            }
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
                    .LoadAggregate<AttributeTest>(p => p.Id == _graph.Id);

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
