using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RefactorThis.GraphDiff.Tests.Models;
using System.Data.Entity;

namespace RefactorThis.GraphDiff.Tests.Tests
{
    [TestClass]
    public class OneMemberGraphBehaviours : TestBase
    {
        [TestMethod]
        public void ShouldAddSingleEntity()
        {
            var node1 = new TestNode
            {
                Title = "Hello"
            };

            using (var context = new TestDbContext())
            {
                node1 = context.UpdateGraph(node1);
                context.SaveChanges();
                Assert.IsNotNull(context.Nodes.SingleOrDefault(p => p.Id == node1.Id));
            }
        }

        [TestMethod]
        public void ShouldUpdateSingleEntity_Detached()
        {
            var node1 = new TestNode
            {
                Title = "Hello"
            };

            using (var context = new TestDbContext())
            {
                context.Nodes.Add(node1);
                context.SaveChanges();
            } // Simulate detach

            node1.Title = "Hello2";

            using (var context = new TestDbContext())
            {
                context.UpdateGraph(node1);
                context.SaveChanges();
                Assert.IsTrue(context.Nodes.Single(p => p.Id == node1.Id).Title == "Hello2");
            }
        }

        //[TestMethod]
        //public void ShouldUpdateSingleEntity_Attached()
        //{
        //    var node1 = new TestNode
        //    {
        //        Title = "Hello"
        //    };

        //    using (var context = new TestDbContext())
        //    {
        //        context.Nodes.Add(node1);
        //        node1.Title = "Hello2";
        //        context.UpdateGraph(node1);
        //        context.SaveChanges();
        //        Assert.IsTrue(context.Nodes.Single(p => p.Id == node1.Id).Title == "Hello2");
        //    }
        //}

        [TestMethod]
        public void ShouldNotUpdateEntityIfNoChangesHaveBeenMade_Detached()
        {
            var node1 = new TestNode
            {
                Title = "Hello"
            };

            using (var context = new TestDbContext())
            {
                node1 = context.Nodes.Add(node1);
                context.SaveChanges();
            } // Simulate detach

            using (var context = new TestDbContext())
            {
                context.UpdateGraph(node1);
                Assert.IsTrue(context.ChangeTracker.Entries().All(p => p.State == EntityState.Unchanged));
                context.SaveChanges();
            }
        }

        //[TestMethod]
        //public void ShouldNotUpdateEntityIfNoChangesHaveBeenMade_Attached()
        //{
        //    var node1 = new TestNode
        //    {
        //        Title = "Hello"
        //    };

        //    using (var context = new TestDbContext())
        //    {
        //        node1 = context.Nodes.Add(node1);
        //        context.UpdateGraph(node1);
        //        Assert.IsTrue(context.ChangeTracker.Entries().All(p => p.State == EntityState.Added));
        //        context.SaveChanges();
        //    }
        //}
    }
}
