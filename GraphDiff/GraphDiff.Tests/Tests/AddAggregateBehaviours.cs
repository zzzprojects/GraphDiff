using Microsoft.VisualStudio.TestTools.UnitTesting;
using RefactorThis.GraphDiff.Tests.Models;
using System.Linq;
using System.Data.Entity;
using System.Collections.Generic;

namespace RefactorThis.GraphDiff.Tests.Tests
{
    [TestClass]
    public class AddAggregateBehaviours
    {
        [TestMethod]
        public void ShouldAddNewAggregateRoot()
        {
            var associated = new OneToManyAssociatedModel { Title = "Associated" };
            var node1 = new TestNode
            {
                Title = "New Node",
                OneToManyOwned = new List<OneToManyOwnedModel>
                {
                    new OneToManyOwnedModel { Title = "One" },
                    new OneToManyOwnedModel { Title = "Two" },
                    new OneToManyOwnedModel { Title = "Three" }
                },
                OneToManyAssociated = new List<OneToManyAssociatedModel>
                {
                    associated
                },
                OneToOneOwned = new OneToOneOwnedModel { Title = "OneToOne" },
                OneToOneAssociated = new OneToOneAssociatedModel { Title = "OneToOneAssociated" }
            };

            using (var context = new TestDbContext())
            {
                context.OneToManyAssociatedModels.Add(associated);
                context.SaveChanges();

                context.Nodes.Add(node1);
                context.SaveChanges();
            } // Simulate detach

            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(node1, map => map
                    .OwnedEntity(p => p.OneToOneOwned)
                    .AssociatedEntity(p => p.OneToOneAssociated)
                    .OwnedCollection(p => p.OneToManyOwned)
                    .AssociatedCollection(p => p.OneToManyAssociated));

                context.SaveChanges();
                var node2 = context.Nodes
                    .Include(p => p.OneToOneOwned)
                    .Include(p => p.OneToOneAssociated)
                    .Include(p => p.OneToManyOwned)
                    .Include(p => p.OneToManyAssociated)
                    .Single(p => p.Id == node1.Id);
                Assert.IsNotNull(node2);
            }
        }
    }
}
