using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RefactorThis.GraphDiff.Internal;
using RefactorThis.GraphDiff.Tests.Models;
using NSubstitute;
using RefactorThis.GraphDiff.Internal.Caching;
using RefactorThis.GraphDiff.Internal.Graph;
using System.Linq.Expressions;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

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
            _cacheProvider.GetOrAdd<GraphNode>(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Func<GraphNode>>())
                .Returns(ci =>
                {
                    return ((Func<GraphNode>)ci[2]).Invoke();
                });

            _register = new AggregateRegister(_cacheProvider);
        }

        [TestMethod]
        public void Register_ShouldReadAggregateAttributesOnSingleModel()
        {
            var entityGraph = _register.GetEntityGraph<AttributeTest>();
            Assert.IsTrue(entityGraph.Members.Count == 2);
            Assert.IsTrue(entityGraph.Members.Pop().Members.Count == 0);
            Assert.IsTrue(entityGraph.Members.Pop().Members.Count == 2);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void GetEntityGraph_ShouldThrowIfMappingSchemeNotFound()
        {
            _register.GetEntityGraph<AttributeTest>("SomeSchemeName");
        }

        // todo add more tests
    }
}
