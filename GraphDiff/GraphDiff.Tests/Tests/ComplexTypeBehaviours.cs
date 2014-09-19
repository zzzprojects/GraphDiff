﻿using System.Data.Entity;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RefactorThis.GraphDiff.Tests.Models;

namespace RefactorThis.GraphDiff.Tests.Tests
{
    [TestClass]
    public class ComplexTypeBehaviours : TestBase
    {
        [TestMethod]
        public void ShouldInsertValues()
        {
            var node1 = new TestNodeWithComplexType { Title = "New Node", ComplexValue = new ComplexValue { Title = "Complex", Value = 5 } };

            using (var context = new TestDbContext())
            {
                // Setup mapping
                node1 = context.UpdateGraph(node1);

                context.SaveChanges();

                var node2 = context.NodesWithComplexType.Single(p => p.Id == node1.Id);

                Assert.IsNotNull(node2);
                Assert.AreEqual("Complex", node2.ComplexValue.Title);
                Assert.AreEqual(5, node2.ComplexValue.Value);
            }
        }

        [TestMethod]
        public void ShouldUpdateValues()
        {
            var node1 = new TestNodeWithComplexType { Title = "New Node", ComplexValue = new ComplexValue { Title = "Complex", Value = 5 } };

            using (var context = new TestDbContext())
            {
                context.NodesWithComplexType.Add(node1);
                context.SaveChanges();
            } // Simulate detach

            node1.ComplexValue = new ComplexValue { Title = "New complex", Value = 10 };
            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(node1);

                context.SaveChanges();
                var node2 = context.NodesWithComplexType.Single(p => p.Id == node1.Id);
                Assert.IsNotNull(node2);
                Assert.IsTrue(node2.ComplexValue.Title == "New complex" && node2.ComplexValue.Value == 10);
            }
        }

        [TestMethod]
        public void ShouldUpdateValuesOnChild()
        {
            var node1 = new TestNodeWithChildWithComplexValue
            {
                ChildWithComplexValue =
                    new TestNodeWithComplexType { Title = "Complex", ComplexValue = new ComplexValue { Title = "Complex", Value = 5 } }
            };

            using (var context = new TestDbContext())
            {
                context.NodesWithChildWithComplexType.Add(node1);
                context.SaveChanges();
            } // Simulate detach

            node1.ChildWithComplexValue.ComplexValue = new ComplexValue { Title = "New complex", Value = 10 };
            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(node1, map => map.OwnedEntity(n => n.ChildWithComplexValue));

                context.SaveChanges();
                var node2 = context.NodesWithChildWithComplexType.Single(p => p.Id == node1.Id);
                Assert.IsNotNull(node2);
                Assert.IsTrue(node2.ChildWithComplexValue.ComplexValue.Title == "New complex" && node2.ChildWithComplexValue.ComplexValue.Value == 10);
            }
        }

        [TestMethod]
        public void ShouldUpdateValuesOnChildren()
        {
            var node1 = new TestNodeWithChildrenWithComplexValue
            {
                ComplexValue = new ComplexValue{Title = "Complex", Value = 5},
                ChildrenWithComplexValue = new[]{
                    new TestNodeWithComplexType { Title = "Complex", ComplexValue = new ComplexValue { Title = "Complex", Value = 5 } },
                    new TestNodeWithComplexType { Title = "Complex2", ComplexValue = new ComplexValue { Title = "Complex", Value = 5 } }
}
            };

            using (var context = new TestDbContext())
            {
                node1 = context.NodesWithChildrenWithComplexType.Add(node1);
                context.SaveChanges();
            } // Simulate detach

            using (var context = new TestDbContext())
            {
                node1 = context.NodesWithChildrenWithComplexType
                    .Include(n => n.ChildrenWithComplexValue)
                    .AsNoTracking()
                    .Single(p => p.Id == node1.Id);

                node1.ChildrenWithComplexValue = new[]
                {
                    new TestNodeWithComplexType
                    {
                        Title = "New complex",
                        ComplexValue = new ComplexValue {Title = "New complex", Value = 10}
                    }
                };
                // Setup mapping
                context.UpdateGraph(node1, map => map
                    .OwnedCollection(n => n.ChildrenWithComplexValue));

                context.SaveChanges();
                var node2 = context.NodesWithChildrenWithComplexType.Single(p => p.Id == node1.Id);

                Assert.IsNotNull(node2);
                Assert.AreEqual(1, node2.ChildrenWithComplexValue.Count());
                Assert.AreEqual("New complex", node2.ChildrenWithComplexValue.First().ComplexValue.Title);
                Assert.AreEqual(10, node2.ChildrenWithComplexValue.First().ComplexValue.Value);
            }
        }

        [TestMethod]
        public void ShouldInsertValuesOnChildren()
        {
            var node1 = new TestNodeWithChildrenWithComplexValue
            {
                ComplexValue = new ComplexValue{Title = "Complex", Value = 5},
                ChildrenWithComplexValue = new[]{
                    new TestNodeWithComplexType { Title = "Complex", ComplexValue = new ComplexValue { Title = "Complex", Value = 5 } }
}
            };

            using (var context = new TestDbContext())
            {
                // Setup mapping
                node1 = context.UpdateGraph(node1, map => map
                    .OwnedCollection(n => n.ChildrenWithComplexValue));

                context.SaveChanges();
                var node2 = context.NodesWithChildrenWithComplexType.Single(p => p.Id == node1.Id);
                
                Assert.IsNotNull(node2);
                Assert.AreEqual(1, node2.ChildrenWithComplexValue.Count());
                Assert.AreEqual("Complex", node2.ChildrenWithComplexValue.First().ComplexValue.Title);
                Assert.AreEqual(5, node2.ChildrenWithComplexValue.First().ComplexValue.Value);
            }
        }
    }
}