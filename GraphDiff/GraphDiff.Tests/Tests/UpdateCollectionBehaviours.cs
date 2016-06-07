using System.Data.Entity;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RefactorThis.GraphDiff.Tests.Models;

namespace RefactorThis.GraphDiff.Tests.Tests
{
    [TestClass]
    public class UpdateCollectionBehaviours : TestBase
    {
        [TestMethod]
        public void ShouldAddSingleEntities()
        {
            var nodes = Enumerable.Range(1, 100)
                .Select(i => new TestNode { Title = "Node" + i })
                .ToArray();

            using (var context = new TestDbContext())
            {
                var savedNodes = context.UpdateGraphs(nodes);
                context.SaveChanges();

                foreach (var node in savedNodes)
                    Assert.IsNotNull(context.Nodes.SingleOrDefault(p => p.Id == node.Id));
            }
        }

        [TestMethod]
        public void ShouldUpdateSingleEntities_Detached()
        {
            var nodes = Enumerable.Range(1, 100)
                .Select(i => new TestNode { Title = "Node" + i })
                .ToArray();

            using (var context = new TestDbContext())
            {
                foreach (var node in nodes)
                    context.Nodes.Add(node);
                context.SaveChanges();
            } // Simulate detach

            foreach (var node in nodes)
                node.Title += "x";

            using (var context = new TestDbContext())
            {
                context.UpdateGraphs(nodes);
                context.SaveChanges();

                foreach (var node in nodes)
                    Assert.IsTrue(context.Nodes.Single(p => p.Id == node.Id).Title.EndsWith("x"));
            }
        }
    }
}