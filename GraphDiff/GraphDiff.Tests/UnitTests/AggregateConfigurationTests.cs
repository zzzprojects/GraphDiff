using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RefactorThis.GraphDiff.Aggregates;
using RefactorThis.GraphDiff.Tests.Models;

namespace RefactorThis.GraphDiff.Tests.UnitTests
{
    [TestClass]
    public class AggregateConfigurationTests
    {
        [TestInitialize]
        public void Init()
        {
            AggregateConfiguration.Aggregates.ClearAll();
        }

        [TestMethod]
        public void Test()
        {
            AggregateConfiguration.Aggregates
                .Register<TestNode>(p =>
                    p.OwnedEntity(m => m.OneToOneOwned)
                )
                .Register<TestNode>("Test.OneToManyAssociated.PartialUpdate", p =>
                    p.AssociatedCollection(m => m.OneToManyAssociated)
                );
        }

        // TODO TESTS
    }
}
