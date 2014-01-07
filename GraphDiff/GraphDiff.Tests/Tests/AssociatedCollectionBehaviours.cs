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
