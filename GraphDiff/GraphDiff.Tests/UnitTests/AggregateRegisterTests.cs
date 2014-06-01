using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RefactorThis.GraphDiff.Internal;
using RefactorThis.GraphDiff.Tests.Models;
using NSubstitute;
using RefactorThis.GraphDiff.Internal.Caching;
using RefactorThis.GraphDiff.Internal.Graph;

namespace RefactorThis.GraphDiff.Tests.UnitTests
{
    [TestClass]
    public class AggregateRegisterTests
    {
        private AggregateRegister _register;
        private ICacheProvider _cacheProvider;

        [TestInitialize]
        public void Initialize()
        {
            _cacheProvider = Substitute.For<ICacheProvider>();
            _cacheProvider.GetOrAdd<GraphNode>(Arg.Any<string>(), Arg.Any<Func<GraphNode>>())
                .Returns(ci =>
                {
                    return ((Func<GraphNode>)ci[1]).Invoke();
                });

            _register = new AggregateRegister(_cacheProvider);
        }

        [TestMethod]
        public void ShouldReadAggregateAttributesOnSingleModel()
        {
            var entityGraph = _register.GetEntityGraph(typeof(AttributeTest));
            Assert.IsTrue(entityGraph.Members.Count == 2);
            Assert.IsTrue(entityGraph.Members.Pop().Members.Count == 0);
            Assert.IsTrue(entityGraph.Members.Pop().Members.Count == 2);
        }
    }
}
