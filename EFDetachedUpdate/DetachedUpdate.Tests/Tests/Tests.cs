using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RefactorThis.EFExtensions;
using System.Data.Entity;

namespace RefactorThis.DetachedUpdate.Tests
{
    /// <summary>
    /// Tests
    /// </summary>
    [TestClass]
    public class Tests
    {
        public Tests()
        {
            Database.SetInitializer<TestDbContext>(new DropCreateDatabaseAlways<TestDbContext>());
        }

        [TestInitialize()]
        public void MyTestInitialize()
        {
            using (var context = new TestDbContext())
            {
                var company1 = context.Companies.Add(new Models.Company
                {
                    Name = "Company 1",
                    Contacts = new List<Models.CompanyContact>
                    {
                        new Models.CompanyContact 
                        { 
                            FirstName = "Bob",
                            LastName = "Brown",
                            Infos = new List<Models.ContactInfo>
                            {
                                new Models.ContactInfo
                                {
                                    Description = "Home",
                                    Email = "test@test.com",
                                    PhoneNumber = "0255525255"
                                }
                            }
                        }
                    }
                });

                var company2 = context.Companies.Add(new Models.Company
                {
                    Name = "Company 2",
                    Contacts = new List<Models.CompanyContact>
                    {
                        new Models.CompanyContact 
                        { 
                            FirstName = "Tim",
                            LastName = "Jones",
                            Infos = new List<Models.ContactInfo>
                            {
                                new Models.ContactInfo
                                {
                                    Description = "Work",
                                    Email = "test@test.com",
                                    PhoneNumber = "456456456456"
                                }
                            }
                        }
                    }
                });

                var project1 = context.Projects.Add(new Models.Project
                {
                    Name = "Major Project 1",
                    Deadline = DateTime.Now,
                    Stakeholders = new List<Models.Company> { company2 }
                });

                var project2 = context.Projects.Add(new Models.Project
                {
                    Name = "Major Project 2",
                    Deadline = DateTime.Now,
                    Stakeholders = new List<Models.Company> { company1 }
                });

                var manager1 = context.Managers.Add(new Models.Manager
                {
                    PartKey = "manager1",
                    PartKey2 = 1,
                    FirstName = "Trent"
                });
                var manager2 = context.Managers.Add(new Models.Manager
                {
                    PartKey = "manager2",
                    PartKey2 = 2,
                    FirstName = "Timothy"
                });

                project2.LeadCoordinator = manager2;
                context.SaveChanges();
            }
        }

        [TestCleanup()]
        public void MyTestCleanup()
        {
            using (var context = new TestDbContext())
            {
                context.Database.ExecuteSqlCommand("DELETE FROM Companies");
                context.Database.ExecuteSqlCommand("DBCC CHECKIDENT (Companies, reseed, 1)");
                context.Database.ExecuteSqlCommand("DELETE FROM Projects");
                context.Database.ExecuteSqlCommand("DBCC CHECKIDENT (Projects, reseed, 1)");
                context.Database.ExecuteSqlCommand("DELETE FROM Managers");
                Assert.IsTrue(context.Companies.Count() == 0);
                Assert.IsTrue(context.Projects.Count() == 0);
                Assert.IsTrue(context.Managers.Count() == 0);
            }
        }

        #region Base record update

        [TestMethod]
        public void BaseEntityUpdate()
        {
            Models.Company company1;
            using (var context = new TestDbContext())
            {
                company1 = context.Companies.Single(p => p.Id == 2);
            } // Simulate detach

            company1.Name = "Company #1"; // Change from Company 1 to Company #1

            using (var context = new TestDbContext())
            {
                context.UpdateGraph(company1, null);
                context.SaveChanges();
                Assert.IsTrue(context.Companies.Single(p => p.Id == 2).Name == "Company #1");
            }
        }

        #endregion

        #region Associated Entity

        [TestMethod]
        public void AssociatedEntityWherePreviousValueWasNull()
        {
            Models.Project project;
            Models.Manager coord;
            using (var context = new TestDbContext())
            {
                project = context.Projects
                    .Include(p => p.LeadCoordinator)
                    .Single(p => p.Id == 1);

                coord = context.Managers
                    .Single(p => p.PartKey == "manager1" && p.PartKey2 == 1);

            } // Simulate detach

            project.LeadCoordinator = coord;

            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(project, map => map
                    .AssociatedEntity(p => p.LeadCoordinator));

                context.SaveChanges();
                Assert.IsTrue(context.Projects
                    .Include(p => p.LeadCoordinator)
                    .Single(p => p.Id == 1)
                    .LeadCoordinator.PartKey == coord.PartKey);
            }
        }

        [TestMethod]
        public void AssociatedEntityWhereNewValueIsNull()
        {
            Models.Project project;
            using (var context = new TestDbContext())
            {
                project = context.Projects
                    .Include(p => p.LeadCoordinator)
                    .Single(p => p.Id == 2);

            } // Simulate detach

            project.LeadCoordinator = null;

            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(project, map => map
                    .AssociatedEntity(p => p.LeadCoordinator));

                context.SaveChanges();
                Assert.IsTrue(context.Projects
                    .Include(p => p.LeadCoordinator)
                    .Single(p => p.Id == 2)
                    .LeadCoordinator == null);
            }
        }

        [TestMethod]
        public void AssociatedEntityWherePreviousValueIsNewValue()
        {
            Models.Project project;
            Models.Manager coord;
            using (var context = new TestDbContext())
            {
                project = context.Projects
                    .Include(p => p.LeadCoordinator)
                    .Single(p => p.Id == 2);

                coord = context.Managers
                    .Single(p => p.PartKey == "manager2" && p.PartKey2 == 2);

            } // Simulate detach

            project.LeadCoordinator = coord;

            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(project, map => map
                    .AssociatedEntity(p => p.LeadCoordinator));

                context.SaveChanges();
                Assert.IsTrue(context.Projects
                    .Include(p => p.LeadCoordinator)
                    .Single(p => p.Id == 2)
                    .LeadCoordinator.PartKey == coord.PartKey);
            }
        }

        [TestMethod]
        public void AssociatedEntityWherePreviousValueIsNotNewValue()
        {
            Models.Project project;
            Models.Manager coord;
            using (var context = new TestDbContext())
            {
                project = context.Projects
                    .Include(p => p.LeadCoordinator)
                    .Single(p => p.Id == 2);

                coord = context.Managers
                    .Single(p => p.PartKey == "manager1" && p.PartKey2 == 1);

            } // Simulate detach

            project.LeadCoordinator = coord;

            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(project, map => map
                    .AssociatedEntity(p => p.LeadCoordinator));

                context.SaveChanges();
                Assert.IsTrue(context.Projects
                    .Include(p => p.LeadCoordinator)
                    .Single(p => p.Id == 2)
                    .LeadCoordinator.PartKey == coord.PartKey);
            }
        }

        [TestMethod]
        public void AssociatedEntityValuesShouldNotBeUpdated()
        {
            Models.Project project;
            using (var context = new TestDbContext())
            {
                project = context.Projects
                    .Include(p => p.LeadCoordinator)
                    .Single(p => p.Id == 2);

            } // Simulate detach

            project.LeadCoordinator.FirstName = "Larry";

            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(project, map => map
                    .AssociatedEntity(p => p.LeadCoordinator));

                context.SaveChanges();
                Assert.IsTrue(context.Projects
                    .Include(p => p.LeadCoordinator)
                    .Single(p => p.Id == 2)
                    .LeadCoordinator.FirstName != "Larry");
            }
        }

        [TestMethod]
        public void AssociatedEntityValuesForNewValueShouldNotBeUpdated()
        {
            Models.Project project;
            Models.Manager coord;
            using (var context = new TestDbContext())
            {
                project = context.Projects
                    .Include(p => p.LeadCoordinator)
                    .Single(p => p.Id == 2);

                coord = context.Managers
                    .Single(p => p.PartKey == "manager1" && p.PartKey2 == 1);

            } // Simulate detach

            project.LeadCoordinator = coord;
            coord.FirstName = "Larry";

            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(project, map => map
                    .AssociatedEntity(p => p.LeadCoordinator));

                context.SaveChanges();
                Assert.IsTrue(context.Projects
                    .Include(p => p.LeadCoordinator)
                    .Single(p => p.Id == 2)
                    .LeadCoordinator.FirstName == "Trent");
            }
        }

        #endregion

        #region Owned Entity

        [TestMethod]
        public void OwnedEntityUpdateValues()
        {
            Models.Project project;
            using (var context = new TestDbContext())
            {
                project = context.Projects
                    .Include(p => p.LeadCoordinator)
                    .Single(p => p.Id == 2);

            } // Simulate detach

            project.LeadCoordinator.FirstName = "Tada";

            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(project, map => map
                    .OwnedEntity(p => p.LeadCoordinator));

                context.SaveChanges();
                Assert.IsTrue(context.Projects
                    .Include(p => p.LeadCoordinator)
                    .Single(p => p.Id == 2)
                    .LeadCoordinator.FirstName == "Tada");
            }
        }

        [TestMethod]
        public void OwnedEntityNewEntity()
        {
            Models.Project project;
            using (var context = new TestDbContext())
            {
                project = context.Projects
                    .Include(p => p.LeadCoordinator)
                    .Single(p => p.Id == 2);

            } // Simulate detach

            project.LeadCoordinator = new Models.Manager { FirstName = "Br", PartKey = "TER", PartKey2 = 2 };

            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(project, map => map
                    .OwnedEntity(p => p.LeadCoordinator));

                context.SaveChanges();
                Assert.IsTrue(context.Projects
                    .Include(p => p.LeadCoordinator)
                    .Single(p => p.Id == 2)
                    .LeadCoordinator.PartKey == "TER");
            }
        }

        [TestMethod]
        public void OwnedEntityRemoveEntity()
        {
            Models.Project project;
            using (var context = new TestDbContext())
            {
                project = context.Projects
                    .Include(p => p.LeadCoordinator)
                    .Single(p => p.Id == 2);

            } // Simulate detach

            project.LeadCoordinator = null;

            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(project, map => map
                    .OwnedEntity(p => p.LeadCoordinator));

                context.SaveChanges();
                Assert.IsTrue(context.Projects
                    .Include(p => p.LeadCoordinator)
                    .Single(p => p.Id == 2)
                    .LeadCoordinator == null);
            }
        }

        #endregion

        #region Associated Collection

        [TestMethod]
        public void AssociatedCollectionAdd()
        {
            // don't know what to do about this yet..
            Models.Project project1;
            Models.Company company2;
            using (var context = new TestDbContext())
            {
                project1 = context.Projects
                    .Include(p => p.Stakeholders)
                    .Single(p => p.Id == 2);

                company2 = context.Companies.Single(p => p.Id == 2);
            } // Simulate detach

            project1.Stakeholders.Add(company2);

            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(project1, map => map
                    .AssociatedCollection(p => p.Stakeholders));

                context.SaveChanges();
                Assert.IsTrue(context.Projects
                    .Include(p => p.Stakeholders)
                    .Single(p => p.Id == 2)
                    .Stakeholders.Count == 2);
            }
        }

        [TestMethod]
        public void AssociatedCollectionRemove()
        {
            Models.Project project1;
            using (var context = new TestDbContext())
            {
                project1 = context.Projects
                    .Include(p => p.Stakeholders)
                    .Single(p => p.Id == 2);
            } // Simulate detach

            var company = project1.Stakeholders.First();
            project1.Stakeholders.Remove(company);

            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(project1, map => map
                    .AssociatedCollection(p => p.Stakeholders));

                context.SaveChanges();
                Assert.IsTrue(context.Projects
                    .Include(p => p.Stakeholders)
                    .Single(p => p.Id == 2)
                    .Stakeholders.Count == 0);

                // Ensure does not delete non owned entity
                Assert.IsTrue(context.Companies.Any(p => p.Id == company.Id));
            }
        }

        [TestMethod]
        public void AssociatedCollectionsEntitiesValuesShouldNotBeUpdated()
        {
            Models.Project project1;
            using (var context = new TestDbContext())
            {
                project1 = context.Projects
                    .Include(p => p.Stakeholders)
                    .Single(p => p.Id == 2);
            } // Simulate detach

            var company = project1.Stakeholders.First();
            company.Name = "TEST OVERWRITE NAME";

            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(project1, map => map
                    .AssociatedCollection(p => p.Stakeholders));

                context.SaveChanges();
                Assert.IsTrue(context.Projects
                    .Include(p => p.Stakeholders)
                    .Single(p => p.Id == 2)
                    .Stakeholders.First().Name != "TEST OVERWRITE NAME");
            }
        }

        #endregion

        #region Owned Collection

        [TestMethod]
        public void OwnedCollectionUpdate()
        {
            Models.Company company1;
            using (var context = new TestDbContext())
            {
                company1 = context.Companies
                    .Include(p => p.Contacts)
                    .Single(p => p.Id == 2);
            } // Simulate detach

            company1.Name = "Company #1"; // Change from Company 1 to Company #1
            company1.Contacts.First().FirstName = "Bobby"; // change from bob to bobby

            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(company1, map => map
                    .OwnedCollection(p => p.Contacts));

                context.SaveChanges();
                Assert.IsTrue(context.Companies
                    .Include(p => p.Contacts)
                    .Single(p => p.Id == 2)
                    .Contacts.First()
                    .FirstName == "Bobby");
                Assert.IsTrue(context.Companies
                    .Include(p => p.Contacts)
                    .Single(p => p.Id == 2)
                    .Contacts.First()
                    .LastName == "Brown");
            }
        }

        [TestMethod]
        public void OwnedCollectionAdd()
        {
            Models.Company company1;
            using (var context = new TestDbContext())
            {
                company1 = context.Companies
                    .Include(p => p.Contacts.Select(m => m.Infos))
                    .Single(p => p.Id == 2);
            } // Simulate detach

            company1.Name = "Company #1"; // Change from Company 1 to Company #1
            company1.Contacts.Add(new Models.CompanyContact
            {
                FirstName = "Charlie",
                LastName = "Sheen",
                Infos = new List<Models.ContactInfo>
                {
                    new Models.ContactInfo { PhoneNumber = "123456789", Description = "Home" }
                }
            });

            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(company1, map => map
                    .OwnedCollection(p => p.Contacts, with => with
                        .OwnedCollection(p => p.Infos)));

                context.SaveChanges();
                Assert.IsTrue(context.Companies
                    .Include(p => p.Contacts.Select(m => m.Infos))
                    .Single(p => p.Id == 2)
                    .Contacts.Count == 2);
                Assert.IsTrue(context.Companies
                    .Include(p => p.Contacts.Select(m => m.Infos))
                    .Single(p => p.Id == 2)
                    .Contacts.Any(p => p.LastName == "Sheen"));
            }
        }

        [TestMethod]
        public void OwnedCollectionAddMultiple()
        {
            Models.Company company1;
            using (var context = new TestDbContext())
            {
                company1 = context.Companies
                    .Include(p => p.Contacts.Select(m => m.Infos))
                    .Single(p => p.Id == 2);
            } // Simulate detach

            company1.Name = "Company #1"; // Change from Company 1 to Company #1
            company1.Contacts.Add(new Models.CompanyContact
            {
                FirstName = "Charlie",
                LastName = "Sheen",
                Infos = new List<Models.ContactInfo>
                {
                    new Models.ContactInfo { PhoneNumber = "123456789", Description = "Home" }
                }
            });
            company1.Contacts.Add(new Models.CompanyContact
            {
                FirstName = "Tim",
                LastName = "Sheen"
            });
            company1.Contacts.Add(new Models.CompanyContact
            {
                FirstName = "Emily",
                LastName = "Sheen"
            });
            company1.Contacts.Add(new Models.CompanyContact
            {
                FirstName = "Mr",
                LastName = "Sheen",
                Infos = new List<Models.ContactInfo>
                {
                    new Models.ContactInfo { PhoneNumber = "123456789", Description = "Home" }
                }
            });
            company1.Contacts.Add(new Models.CompanyContact
            {
                FirstName = "Mr",
                LastName = "X"
            });

            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(company1, map => map
                    .OwnedCollection(p => p.Contacts, with => with
                        .OwnedCollection(p => p.Infos)));

                context.SaveChanges();
                Assert.IsTrue(context.Companies
                    .Include(p => p.Contacts.Select(m => m.Infos))
                    .Single(p => p.Id == 2)
                    .Contacts.Count == 6);
            }
        }

        [TestMethod]
        public void OwnedCollectionRemove()
        {
            Models.Company company1;
            using (var context = new TestDbContext())
            {
                company1 = context.Companies
                    .Include(p => p.Contacts.Select(m => m.Infos))
                    .Single(p => p.Id == 2);
            } // Simulate detach

            company1.Contacts.Remove(company1.Contacts.First());

            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(company1, map => map
                    .OwnedCollection(p => p.Contacts, with => with
                        .OwnedCollection(p => p.Infos)));

                context.SaveChanges();
                Assert.IsTrue(context.Companies
                    .Include(p => p.Contacts.Select(m => m.Infos))
                    .Single(p => p.Id == 2)
                    .Contacts.Count == 0);
            }
        }

        [TestMethod]
        public void OwnedCollectionAddRemoveUpdate()
        {
            Models.Company company1;
            using (var context = new TestDbContext())
            {
                company1 = context.Companies
                    .Include(p => p.Contacts.Select(m => m.Infos))
                    .Single(p => p.Id == 2);

                company1.Contacts.Add(new Models.CompanyContact { FirstName = "Hello", LastName = "Test" });
                context.SaveChanges();
            } // Simulate detach

            // Update, remove and add
            company1.Name = "Company #1"; // Change from Company 1 to Company #1

            string originalname = company1.Contacts.First().FirstName;
            company1.Contacts.First().FirstName = "Terrrrrry";

            company1.Contacts.Remove(company1.Contacts.Skip(1).First());

            company1.Contacts.Add(new Models.CompanyContact
            {
                FirstName = "Charlie",
                LastName = "Sheen",
                Infos = new List<Models.ContactInfo>
                {
                    new Models.ContactInfo { PhoneNumber = "123456789", Description = "Home" }
                }
            });

            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(company1, map => map
                    .OwnedCollection(p => p.Contacts, with => with
                        .OwnedCollection(p => p.Infos)));

                context.SaveChanges();

                var test = context.Companies
                    .Include(p => p.Contacts.Select(m => m.Infos))
                    .Single(p => p.Id == 2);


                Assert.IsTrue(test.Contacts.Count == 2);
                Assert.IsTrue(test.Contacts.First().FirstName == "Terrrrrry");
                Assert.IsTrue(test.Contacts.Skip(1).First().FirstName == "Charlie");
            }
        }

        [TestMethod]
        public void OwnedCollectionWithOwnedCollection()
        {
            Models.Company company1;
            using (var context = new TestDbContext())
            {
                company1 = context.Companies
                    .Include(p => p.Contacts.Select(m => m.Infos))
                    .First();
            } // Simulate detach

            company1.Contacts.First().Infos.First().Email = "testeremail";
            company1.Contacts.First().Infos.Add(new Models.ContactInfo { Description = "Test", Email = "test@test.com" });

            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(company1, map => map
                    .OwnedCollection(p => p.Contacts, with => with
                        .OwnedCollection(m => m.Infos)));

                context.SaveChanges();
                var value = context.Companies.Include(p => p.Contacts.Select(m => m.Infos))
                    .First();

                Assert.IsTrue(value.Contacts.First().Infos.Count == 2);
                Assert.IsTrue(value.Contacts.First().Infos.First().Email == "testeremail");
            }
        }

        #endregion

        // TODO Incomplete. Please report any bugs to GraphDiff on github.
        // Will add more tests when I have time.

    }
}
