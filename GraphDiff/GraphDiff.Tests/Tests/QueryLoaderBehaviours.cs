using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RefactorThis.GraphDiff.Tests.Models;
using System;
using System.Data.Entity;
using System.Linq;

namespace RefactorThis.GraphDiff.Tests.Tests
{
    [TestClass]
    public class QueryLoaderBehaviours : TestBase
    {
        private TestNode _node;
        private OneToOneOneToOneAssociatedModel _oneToOneAssociated;
        private OneToOneOneToManyAssociatedModel _oneToManyAssociated;

        [TestInitialize]
        public void Init()
        {
            // setup
            _oneToOneAssociated = new OneToOneOneToOneAssociatedModel { Title = "Associated Update" };
            _oneToManyAssociated = new OneToOneOneToManyAssociatedModel { Title = "Many Associated Update" };
            _node = new TestNode
            {
                Title = "New Node",
                OneToOneOwned = new OneToOneOwnedModel
                {
                    OneToOneOneToOneAssociated = new OneToOneOneToOneAssociatedModel { Title = "Hello" },
                    OneToOneOneToOneOwned = new OneToOneOneToOneOwnedModel { Title = "Hello" },
                    OneToOneOneToManyAssociated = new List<OneToOneOneToManyAssociatedModel>
                    {
                        new OneToOneOneToManyAssociatedModel { Title = "Hello" },
                        new OneToOneOneToManyAssociatedModel { Title = "Hello" }
                    },
                    OneToOneOneToManyOwned = new List<OneToOneOneToManyOwnedModel>
                    {
                        new OneToOneOneToManyOwnedModel { Title = "Hello" },
                        new OneToOneOneToManyOwnedModel { Title = "Hello" },
                        new OneToOneOneToManyOwnedModel { Title = "Hello" }
                    }
                }
            };
        }

        [TestMethod]
        public void ShouldPerformSingleQueryWhenRequested()
        {
            using (var context = new TestDbContext())
            {
                context.Set<OneToOneOneToOneAssociatedModel>().Add(_oneToOneAssociated);
                context.Set<OneToOneOneToManyAssociatedModel>().Add(_oneToManyAssociated);
                context.Nodes.Add(_node);
                context.SaveChanges();
            }
            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(
                    _node,
                    map => map.OwnedEntity(p => p.OneToOneOwned, with => with
                        .OwnedEntity(p => p.OneToOneOneToOneOwned)
                        .AssociatedEntity(p => p.OneToOneOneToOneAssociated)
                        .OwnedCollection(p => p.OneToOneOneToManyOwned)
                        .AssociatedCollection(p => p.OneToOneOneToManyAssociated)),
                    new UpdateParams { QueryMode = QueryMode.SingleQuery });
            }

            // TODO how do I test number of queries..
        }

        [TestMethod]
        public void ShouldPerformMutlipleQueriesWhenRequested()
        {
            using (var context = new TestDbContext())
            {
                context.Set<OneToOneOneToOneAssociatedModel>().Add(_oneToOneAssociated);
                context.Set<OneToOneOneToManyAssociatedModel>().Add(_oneToManyAssociated);
                context.Nodes.Add(_node);
                context.SaveChanges();
            }

            var x = new LocalDbConnectionFactory("v11.0");
            var connection = x.CreateConnection("GraphDiff");

            using (var context = new TestDbContext(connection))
            {
                // Setup mapping
                context.UpdateGraph(
                    entity: _node,
                    mapping: map => map.OwnedEntity(p => p.OneToOneOwned, with => with
                        .OwnedEntity(p => p.OneToOneOneToOneOwned)
                        .AssociatedEntity(p => p.OneToOneOneToOneAssociated)
                        .OwnedCollection(p => p.OneToOneOneToManyOwned)
                        .AssociatedCollection(p => p.OneToOneOneToManyAssociated)),
                    updateParams: new UpdateParams { QueryMode = QueryMode.MultipleQuery });
            }

            // TODO how do I test number of queries..
        }

        [TestMethod]
        public void ShouldPrametrizeLoadingQuery()
        {
            using (var context = new TestDbContext())
            {
                context.Set<OneToOneOneToOneAssociatedModel>().Add(_oneToOneAssociated);
                context.Set<OneToOneOneToManyAssociatedModel>().Add(_oneToManyAssociated);
                context.Nodes.Add(_node);
                context.SaveChanges();
            }

            var x = new LocalDbConnectionFactory("v11.0");
            var connection = x.CreateConnection("GraphDiff");

            using (var context = new TestDbContext(connection))
            {
                using (var logCollector = new QueryLogCollector(context))
                {
                    // Setup mapping
                    context.UpdateGraph(
                        entity: _node,
                        mapping: map => map.OwnedEntity(p => p.OneToOneOwned, with => with
                            .OwnedEntity(p => p.OneToOneOneToOneOwned)
                            .AssociatedEntity(p => p.OneToOneOneToOneAssociated)
                            .OwnedCollection(p => p.OneToOneOneToManyOwned)
                            .AssociatedCollection(p => p.OneToOneOneToManyAssociated)),
                        updateParams: new UpdateParams { QueryMode = QueryMode.SingleQuery });

                    Assert.IsTrue(logCollector.Logs.Any(l => l.Contains("@p__linq__0")),
                        "Can't find a parameter in the loading query");
                }
            }

            // TODO how do I test number of queries..
        }
    }

    internal class QueryLogCollector : IDisposable
    {
        private readonly DbContext _context;
        private readonly Action<string> _originalLogger;
        private readonly List<string> _logs = new List<string>();

        public List<string> Logs
        {
            get { return _logs; }
        }

        public QueryLogCollector(DbContext context)
        {
            _context = context;
            _originalLogger = _context.Database.Log;
            _context.Database.Log = line => _logs.Add(line);
        }

        public void Dispose()
        {
            _context.Database.Log = _originalLogger;
        }
    }
}
