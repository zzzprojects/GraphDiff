using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RefactorThis.GraphDiff.Tests.Models;

namespace RefactorThis.GraphDiff.Tests.Tests
{
    [TestClass]
    public class ErrorHandlingBehaviours : TestBase
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

        [TestMethod]
        public void ShouldThrowIfCollectionIsMappedAsEntity()
        {
            using (var context = new TestDbContext())
            {
                ArgumentException argumentException = null;
                try
                {
                    context.UpdateGraph(new TestNode(), map => map.OwnedEntity(n => n.OneToManyAssociated));
                }
                catch (ArgumentException exception)
                {
                    argumentException = exception;
                }

                Assert.IsNotNull(argumentException);
                Assert.IsTrue(argumentException.Message.Contains("OneToManyAssociated"));
                Assert.IsTrue(argumentException.Message.Contains("owned"));
                argumentException = null;

                try
                {
                    context.UpdateGraph(new TestNode(), map => map.AssociatedEntity(n => n.OneToManyAssociated));
                }
                catch (ArgumentException exception)
                {
                    argumentException = exception;
                }

                Assert.IsNotNull(argumentException);
                Assert.IsTrue(argumentException.Message.Contains("OneToManyAssociated"));
                Assert.IsTrue(argumentException.Message.Contains("associated"));
            }
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void ShouldThrowBecauseOfAmbiguousUnmappedParentRelation()
        {
            var firstParent = new OneToManyOwnedParentModel { Title = "First Parent" };
            var child = new OneToManyOwnedMultipleParentsModel { FirstParent = firstParent, Title = "Child" };
            firstParent.OneToManyOwnedMultipleParentsModels = new List<OneToManyOwnedMultipleParentsModel> { child };

            using (var context = new TestDbContext())
            {
                context.UpdateGraph(firstParent, a => a.OwnedCollection(b => b.OneToManyOwnedMultipleParentsModels));
            }
        }
    }
}
