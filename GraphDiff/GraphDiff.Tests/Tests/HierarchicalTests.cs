namespace RefactorThis.GraphDiff.Tests.Tests
{
    using System.Data.Entity;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using RefactorThis.GraphDiff.Tests.Models;

    [TestClass]
    public class HierarchicalTests
    {
        [TestMethod]
        public void Proof_That_A_Hierachy_Fails_to_Save()
        {
            // Create the following hierarchy:
            // Root
            //   |
            //   +-- OneToManyHierarchical (topLevel1)
            //   |             +-- OneToManyHierarchical (topLevel1_sub1)
            //   |             +-- OneToManyHierarchical (topLevel1_sub2)
            //   |
            //   +-- OneToManyHierarchical (topLevel2)
            //                 +-- OneToManyHierarchical (topLevel2_sub1)
            //                 +-- OneToManyHierarchical (topLevel2_sub2)

            // but after saving this using graphdiff, the database contains this:
            // Root
            //   |
            //   +-- OneToManyHierarchical (topLevel1)
            //   |             +-- OneToManyHierarchical (topLevel1_sub1)
            //   |             +-- OneToManyHierarchical (topLevel1_sub2)
            //   |             +-- OneToManyHierarchical (topLevel2_sub1)
            //   |             +-- OneToManyHierarchical (topLevel2_sub2)
            //   |
            //   +-- OneToManyHierarchical (topLevel2)

            var root = new RootModel();

            var topLevel1 = new OneToManyHierarchicalModel();
            var topLevel1_sub1 = new OneToManyHierarchicalModel { Parent = topLevel1 };
            var topLevel1_sub2 = new OneToManyHierarchicalModel { Parent = topLevel1 };

            var topLevel2 = new OneToManyHierarchicalModel();
            var topLevel2_sub1 = new OneToManyHierarchicalModel { Parent = topLevel1 };
            var topLevel2_sub2 = new OneToManyHierarchicalModel { Parent = topLevel1 };

            root.Children.Add(topLevel1);
            root.Children.Add(topLevel1_sub1);
            root.Children.Add(topLevel1_sub2);

            root.Children.Add(topLevel2);
            root.Children.Add(topLevel2_sub1);
            root.Children.Add(topLevel2_sub2);

            using (var dataContext = new TestDbContext())
            {
                root = dataContext.UpdateGraph(root, rootMap => rootMap.OwnedCollection(r => r.Children, childrenMap => childrenMap.AssociatedEntity(li => li.Parent)));
                dataContext.SaveChanges();

                RootModel loaded = dataContext.RootModels.Include(o => o.Children).Single(o => o.Id == root.Id);

                Assert.AreEqual(6, loaded.Children.Count());

                foreach (var topLevel in loaded.Children.Where(li => li.Parent == null))
                {
                    // fails: all sub-Entites will have been moved to the first top level item (expected:<2>. Actual:<4>. )
                    Assert.AreEqual(2, loaded.Children.Count(li => li.ParentId == topLevel.Id));
                }
            }
        }
    }
}
