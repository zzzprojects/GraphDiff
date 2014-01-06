using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq.Expressions;
using RefactorThis.GraphDiff.Tests.Models;
using System.Data.Entity;
using System.Linq;

namespace RefactorThis.GraphDiff.Tests.Tests
{
    [TestClass]
    public class ConfigurationVisitorBehaviours : TestBase
    {
        public Expression<Func<TestNode, OneToOneOwnedModel>> Lambda { get; set; }

        [TestMethod]
        public void ShouldBeAbleToVisitExpressionsStoredAsFields()
        {
            var node1 = new TestNode
            {
                Title = "One",
                OneToOneOwned = new OneToOneOwnedModel { Title = "Hello" }
            };

            using (var context = new TestDbContext())
            {
                context.Nodes.Add(node1);
                context.SaveChanges();
            } // Simulate detach

            node1.OneToOneOwned.Title = "Hey2";

            Expression<Func<TestNode, OneToOneOwnedModel>> lambda = (p => p.OneToOneOwned);
            Expression<Func<IUpdateConfiguration<TestNode>, dynamic>> exp = map => map.OwnedEntity(lambda);

            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(node1, exp);

                context.SaveChanges();
                Assert.IsTrue(context.Nodes
                    .Include(p => p.OneToOneOwned)
                    .Single(p => p.Id == node1.Id)
                    .OneToOneOwned.Title == "Hey2");
            }
        }

        [TestMethod]
        public void ShouldBeAbleToVisitExpressionsStoredAsProperties()
        {
            var node1 = new TestNode
            {
                Title = "One",
                OneToOneOwned = new OneToOneOwnedModel { Title = "Hello" }
            };

            using (var context = new TestDbContext())
            {
                context.Nodes.Add(node1);
                context.SaveChanges();
            } // Simulate detach

            node1.OneToOneOwned.Title = "Hey2";

            Lambda = (p => p.OneToOneOwned);
            Expression<Func<IUpdateConfiguration<TestNode>, dynamic>> exp = map => map.OwnedEntity(Lambda);

            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(node1, exp);

                context.SaveChanges();
                Assert.IsTrue(context.Nodes
                    .Include(p => p.OneToOneOwned)
                    .Single(p => p.Id == node1.Id)
                    .OneToOneOwned.Title == "Hey2");
            }
        }
    }
}
