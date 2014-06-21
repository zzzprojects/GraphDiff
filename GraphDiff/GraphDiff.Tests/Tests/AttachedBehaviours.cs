using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RefactorThis.GraphDiff.Tests.Models;

namespace RefactorThis.GraphDiff.Tests.Tests
{
    [TestClass]
    public class AttachedBehaviours : TestBase
    {
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ShouldThrowExceptionIfAggregateIsNotDetached()
        {
            using (var context = new TestDbContext())
            {
                var node = new TestNode();
                context.Nodes.Add(node);
                node.Title = "Hello";
                context.UpdateGraph(node);
                context.SaveChanges();
            }
        }
    }
}
