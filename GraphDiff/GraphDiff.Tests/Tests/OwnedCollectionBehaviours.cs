using Microsoft.VisualStudio.TestTools.UnitTesting;
using RefactorThis.GraphDiff.Tests.Models;
using System.Linq;
using System.Data.Entity;
using System.Collections.Generic;

namespace RefactorThis.GraphDiff.Tests.Tests
{
    //     * owned collection
    //     *  - add items
    //     *  - remove items
    //     *  - update items
    //     *  - empty collection
    //     *  - completely new collection
    //     *  - with associated entity
    //     *  - with owned entity
    //     *  - with associated collection
    //     *  - with owned collection
    [TestClass]
    public class OwnedCollectionBehaviours : TestBase
    {
        [TestMethod]
        public void ShouldUpdateItemInOwnedCollection()
        {
            var node1 = new TestNode
            {
                Title = "New Node",
                OneToManyOwned = new List<OneToManyOwnedModel>
                {
                    new OneToManyOwnedModel { Title = "Hello" }
                }
            };

            using (var context = new TestDbContext())
            {
                context.Nodes.Add(node1);
                context.SaveChanges();
            } // Simulate detach

            node1.OneToManyOwned.First().Title = "What's up";
            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(node1, map => map
                    .OwnedCollection(p => p.OneToManyOwned));

                context.SaveChanges();
                var node2 = context.Nodes.Include(p => p.OneToManyOwned).Single(p => p.Id == node1.Id);
                Assert.IsNotNull(node2);
                var owned = node2.OneToManyOwned.First();
                Assert.IsTrue(owned.OneParent == node2 && owned.Title == "What's up");
            }
        }

        [TestMethod]
        public void ShouldAddNewItemInOwnedCollection()
        {
            var node1 = new TestNode
            {
                Title = "New Node",
                OneToManyOwned = new List<OneToManyOwnedModel>
                {
                    new OneToManyOwnedModel { Title = "Hello" }
                }
            };

            using (var context = new TestDbContext())
            {
                context.Nodes.Add(node1);
                context.SaveChanges();
            } // Simulate detach

            var newModel = new OneToManyOwnedModel { Title = "Hi" };
            node1.OneToManyOwned.Add(newModel);
            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(node1, map => map
                    .OwnedCollection(p => p.OneToManyOwned));

                context.SaveChanges();
                var node2 = context.Nodes.Include(p => p.OneToManyOwned).Single(p => p.Id == node1.Id);
                Assert.IsNotNull(node2);
                Assert.IsTrue(node2.OneToManyOwned.Count == 2);
                var owned = context.OneToManyOwnedModels.Single(p => p.Id == newModel.Id);
                Assert.IsTrue(owned.OneParent == node2 && owned.Title == "Hi");
            }
        }

        [TestMethod]
        public void ShouldRemoveItemsInOwnedCollection()
        {
            var node1 = new TestNode
            {
                Title = "New Node",
                OneToManyOwned = new List<OneToManyOwnedModel>
                {
                    new OneToManyOwnedModel { Title = "Hello" },
                    new OneToManyOwnedModel { Title = "Hello2" },
                    new OneToManyOwnedModel { Title = "Hello3" }
                }
            };

            using (var context = new TestDbContext())
            {
                context.Nodes.Add(node1);
                context.SaveChanges();
            } // Simulate detach

            node1.OneToManyOwned.Remove(node1.OneToManyOwned.First());
            node1.OneToManyOwned.Remove(node1.OneToManyOwned.First());
            node1.OneToManyOwned.Remove(node1.OneToManyOwned.First());
            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(node1, map => map
                    .OwnedCollection(p => p.OneToManyOwned));

                context.SaveChanges();
                var node2 = context.Nodes.Include(p => p.OneToManyOwned).Single(p => p.Id == node1.Id);
                Assert.IsNotNull(node2);
                Assert.IsTrue(node2.OneToManyOwned.Count == 0);
            }
        }

        [TestMethod]
        public void ShouldMergeTwoCollectionsAndDecideOnUpdatesDeletesAndAdds()
        {
            var node1 = new TestNode
            {
                Title = "New Node",
                OneToManyOwned = new List<OneToManyOwnedModel>
                {
                    new OneToManyOwnedModel { Title = "This" },
                    new OneToManyOwnedModel { Title = "Is" },
                    new OneToManyOwnedModel { Title = "A" },
                    new OneToManyOwnedModel { Title = "Test" }
                }
            };

            using (var context = new TestDbContext())
            {
                context.Nodes.Add(node1);
                context.SaveChanges();
            } // Simulate detach

            node1.OneToManyOwned.Remove(node1.OneToManyOwned.First());
            node1.OneToManyOwned.First().Title = "Hello";
            node1.OneToManyOwned.Add(new OneToManyOwnedModel { Title = "Finish" });
            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(node1, map => map
                    .OwnedCollection(p => p.OneToManyOwned));

                context.SaveChanges();
                var node2 = context.Nodes.Include(p => p.OneToManyOwned).Single(p => p.Id == node1.Id);
                Assert.IsNotNull(node2);
                var list = node2.OneToManyOwned.ToList();
                Assert.IsTrue(list[0].Title == "Hello");
                Assert.IsTrue(list[1].Title == "A");
                Assert.IsTrue(list[2].Title == "Test");
                Assert.IsTrue(list[3].Title == "Finish");
            }
        }


        /*
         *         #region Child List of items that use a Base class that contain their Id/Key
        [TestMethod]
        public void ModifyListOfItemsWithNonMappedBaseClass()
        {
            int id;
            byte[] rowVersion;
            using (var db = new TestDbContext())
            {
                var contact = new Contact
                {
                    Name = "Test",
                    ContactInfos = new List<ContactContactInfo>
                    {
                        new ContactContactInfo
                        {
                            Type = "Phone",
                            Value = "1234567890"
                        },
                        new ContactContactInfo
                        {
                            Type = "Email",
                            Value = "a@a.com"
                        }
                    }
                };
                db.Contacts.Add(contact);
                db.SaveChanges();
                id = contact.Id;
                rowVersion = contact.RowVersion;
            }

            using (var db = new TestDbContext())
            {
                var contact = new Contact
                {
                    RowVersion = rowVersion,
                    Id = id,
                    Name = "Test2",
                    ContactInfos = new List<ContactContactInfo>
                    {
                        new ContactContactInfo
                        {
                            Type = "Email",
                            Value = "b@b.com"
                        }
                    }
                };

                db.UpdateGraph(contact, map =>
                    map.OwnedCollection(c => c.ContactInfos));
                db.SaveChanges();
            }
        }

        [TestMethod]
        public void EnsureANonMappedClassCanBeUsedWithMappedBaseClass()
        {
            int id;
            using (var db = new TestDbContext())
            {
                var item = new NonMappedInheritor
                {
                    FirstName = "Tim"
                };
                db.MappedBase.Add(item);
                db.SaveChanges();
                id = item.Id;
            }

            using (var db = new TestDbContext())
            {
                var item = new NonMappedInheritor
                {
                    Id = id,
                    FirstName = "James"
                };

                db.UpdateGraph(item);
                db.SaveChanges();
                Assert.IsTrue(db.MappedBase.OfType<NonMappedInheritor>().Single(p => p.Id == id).FirstName == "James");
            }
        }

        #endregion
         * */
        //[TestMethod]
        //public void OwnedCollectionWithOwnedCollection()
        //{
        //    Company company1;
        //    using (var context = new TestDbContext())
        //    {
        //        company1 = context.Companies
        //            .Include(p => p.Contacts.Select(m => m.Infos))
        //            .First();
        //    } // Simulate detach

        //    company1.Contacts.First().Infos.First().Email = "testeremail";
        //    company1.Contacts.First().Infos.Add(new ContactInfo { Description = "Test", Email = "test@test.com" });

        //    using (var context = new TestDbContext())
        //    {
        //        // Setup mapping
        //        context.UpdateGraph(company1, map => map
        //            .OwnedCollection(p => p.Contacts, with => with
        //                .OwnedCollection(m => m.Infos)));

        //        context.SaveChanges();
        //        var value = context.Companies.Include(p => p.Contacts.Select(m => m.Infos))
        //            .First();

        //        Assert.IsTrue(value.Contacts.First().Infos.Count == 2);
        //        Assert.IsTrue(value.Contacts.First().Infos.First().Email == "testeremail");
        //    }
        //}

        //// added as per ticket #5
        //// also tried to add some more complication to this graph to ensure everything works well
        //[TestMethod]
        //public void OwnedMultipleLevelCollectionMappingWithAssociatedReload()
        //{
        //    MultiLevelTest multiLevelTest;
        //    Hobby hobby;
        //    using (var context = new TestDbContext())
        //    {
        //        multiLevelTest = context.MultiLevelTest.Add(new MultiLevelTest
        //        {
        //            Managers = new[] // test arrays as well
        //            {
        //                new Manager 
        //                {
        //                    Key = "dasfds",
        //                    PartKey = "xxx",
        //                    PartKey2 = 2,
        //                    Employees = new List<Employee>
        //                    {
        //                        new Employee 
        //                        {
        //                            Key = "xsdf",
        //                            FirstName = "Asdf",
        //                            Hobbies = new List<Hobby>
        //                            {
        //                                new Hobby 
        //                                {
        //                                    HobbyType = "Test hobby type"
        //                                }
        //                            }
        //                         }
        //                     }
        //                }
        //            }
        //        });

        //        hobby = context.Hobbies.Add(new Hobby { HobbyType = "Skiing" });
        //        context.SaveChanges();
        //    } // Simulate detach

        //    // Graph changes

        //    // Should not update changes to hobby
        //    hobby.HobbyType = "Something Else";

        //    // Update changes to manager
        //    var manager = multiLevelTest.Managers.First();
        //    manager.FirstName = "Tester";

        //    // Update changes to employees
        //    var employeeToUpdate = manager.Employees.First();
        //    employeeToUpdate.Hobbies.Clear();
        //    employeeToUpdate.Hobbies.Add(hobby);
        //    manager.Employees.Add(new Employee
        //    {
        //        FirstName = "Tim",
        //        Key = "Tim1",
        //        Manager = multiLevelTest.Managers.First()
        //    });

        //    using (var context = new TestDbContext())
        //    {
        //        GraphDiffConfiguration.ReloadAssociatedEntitiesWhenAttached = true;
        //        // Setup mapping
        //        context.UpdateGraph(multiLevelTest, map => map
        //            .OwnedCollection(x => x.Managers, withx => withx
        //                .AssociatedCollection(pro => pro.Projects)
        //                .OwnedCollection(p => p.Employees, with => with
        //                    .AssociatedCollection(m => m.Hobbies)
        //                    .OwnedEntity(m => m.Locker))));

        //        context.SaveChanges();

        //        GraphDiffConfiguration.ReloadAssociatedEntitiesWhenAttached = false;

        //        var result = context.MultiLevelTest
        //            .Include("Managers.Employees.Hobbies")
        //            .Include("Managers.Employees.Locker")
        //            .Include("Managers.Projects")
        //            .First();

        //        var updateManager = result.Managers.Single(p => p.PartKey == manager.PartKey && p.PartKey2 == manager.PartKey2);
        //        var updateEmployee = updateManager.Employees.Single(p => p.Key == employeeToUpdate.Key);
        //        var updateHobby = context.Hobbies.Single(p => p.Id == hobby.Id);

        //        Assert.IsTrue(updateManager.Employees.Count() == 2);
        //        Assert.IsTrue(result.Managers.First().FirstName == "Tester");
        //        Assert.IsTrue(updateEmployee.Hobbies.Count() == 1);
        //        Assert.IsTrue(updateEmployee.Hobbies.First().HobbyType == "Skiing");
        //        Assert.IsTrue(updateHobby.HobbyType == "Skiing");
        //        Assert.IsTrue(result.Managers.First().Employees.Any(p => p.Key == "Tim1"));
        //    }
        //}

        //[TestMethod]
        //public void OwnedNestedEntityTest()
        //{
        //    OwnedNestedTest test1;
        //    using (var context = new TestDbContext())
        //    {
        //        test1 = new OwnedNestedTest
        //        {
        //            Name = "Test1",
        //            Test2 = new OwnedNestedTest2
        //            {
        //                Name = "Test2",
        //                Test3 = new OwnedNestedTest3
        //                {
        //                    Name = "Test3"
        //                }
        //            }
        //        };

        //        context.UpdateGraph(test1, map => map
        //            .OwnedEntity(p => p.Test2, with => with
        //                .OwnedEntity(p => p.Test3)));

        //        context.SaveChanges();
        //    } // Simulate detach

        //    test1.Name = "Updated";
        //    test1.Test2.Name = "Updated";
        //    test1.Test2.Test3.Name = "Updated";

        //    using (var context = new TestDbContext())
        //    {
        //        // Setup mapping
        //        test1 = context.UpdateGraph(test1, map => map
        //            .OwnedEntity(p => p.Test2, with => with
        //                .OwnedEntity(p => p.Test3)));

        //        context.SaveChanges();
        //        test1 = context.OwnedNestedTests.Include(p => p.Test2.Test3).Single(p => p.Id == test1.Id);
        //        Assert.IsTrue(test1.Name == "Updated");
        //        Assert.IsTrue(test1.Test2.Name == "Updated");
        //        Assert.IsTrue(test1.Test2.Test3.Name == "Updated");
        //    }
        //}

        //// TODO FIXME
        //// need to add test for OwnedEntityGraphNode line 23
        //// shoudl remove old value.

        //#endregion

        //[TestMethod]
        //public void OwnedCollectionAddMultiple()
        //{
        //    Company company1;
        //    using (var context = new TestDbContext())
        //    {
        //        company1 = context.Companies
        //            .Include(p => p.Contacts.Select(m => m.Infos))
        //            .Single(p => p.Id == 2);
        //    } // Simulate detach

        //    company1.Name = "Company #1"; // Change from Company 1 to Company #1
        //    company1.Contacts.Add(new CompanyContact
        //    {
        //        FirstName = "Charlie",
        //        LastName = "Sheen",
        //        Infos = new List<ContactInfo>
        //        {
        //            new ContactInfo { PhoneNumber = "123456789", Description = "Home" }
        //        }
        //    });
        //    company1.Contacts.Add(new CompanyContact
        //    {
        //        FirstName = "Tim",
        //        LastName = "Sheen"
        //    });
        //    company1.Contacts.Add(new CompanyContact
        //    {
        //        FirstName = "Emily",
        //        LastName = "Sheen"
        //    });
        //    company1.Contacts.Add(new CompanyContact
        //    {
        //        FirstName = "Mr",
        //        LastName = "Sheen",
        //        Infos = new List<ContactInfo>
        //        {
        //            new ContactInfo { PhoneNumber = "123456789", Description = "Home" }
        //        }
        //    });
        //    company1.Contacts.Add(new CompanyContact
        //    {
        //        FirstName = "Mr",
        //        LastName = "X"
        //    });

        //    using (var context = new TestDbContext())
        //    {
        //        // Setup mapping
        //        context.UpdateGraph(company1, map => map
        //            .OwnedCollection(p => p.Contacts, with => with
        //                .OwnedCollection(p => p.Infos)));

        //        context.SaveChanges();
        //        Assert.IsTrue(context.Companies
        //            .Include(p => p.Contacts.Select(m => m.Infos))
        //            .Single(p => p.Id == 2)
        //            .Contacts.Count == 6);
        //    }
        //}
    }
}
