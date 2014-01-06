using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RefactorThis.GraphDiff.Tests.Tests
{
    [TestClass]
    public class ErrorHandlingBehaviours
    {
        internal class UnknownType { }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ShouldThrowIfTypeIsNotKnown()
        {
            using (var context = new TestDbContext())
            {
                context.UpdateGraph(new UnknownType());
                context.SaveChanges();
            }
        }
    }
}
