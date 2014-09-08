using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RefactorThis.GraphDiff.Tests.Models;

namespace RefactorThis.GraphDiff.Tests.Tests
{
    [TestClass]
    public class PrivateConstructorBehaviours : TestBase
    {
        [TestMethod]
        public void ShouldCreateEntityWithPrivateConstructor()
        {
            var node1 = TestNodeWithPrivateConstructor.Create("Hello");

            using (var context = new TestDbContext())
            {
                node1 = context.UpdateGraph(node1);
                context.SaveChanges();
                Assert.IsNotNull(context.NodesWithPrivateConstructor.SingleOrDefault(p => p.Id == node1.Id));
            }
        }
    }
}
