using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using System.Transactions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RefactorThis.GraphDiff.Tests.Models;

namespace RefactorThis.GraphDiff.Tests
{
	/// <summary>
	/// Tests
	/// </summary>
	[TestClass]
	public class Tests
	{
		#region Class construction & initialization

		private TransactionScope _transactionScope;

		public Tests()
		{
			Database.SetInitializer<TestDbContext>(new DropCreateDatabaseAlways<TestDbContext>());
		}

		[ClassInitialize]
		public static void SetupTheDatabase(TestContext testContext)
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
					Key = "sdfsdf",
					PartKey = "manager1",
					PartKey2 = 1,
					FirstName = "Trent"
				});
				var manager2 = context.Managers.Add(new Models.Manager
				{
					Key = "bvdvsd",
					PartKey = "manager2",
					PartKey2 = 2,
					FirstName = "Timothy"
				});

				var locker1 = new Models.Locker
				{
					Combination = "Asdfasdf",
					Location = "Middle Earth"
				};

				var employee = new Models.Employee
				{
					Manager = manager1,
					Key = "Asdf",
					FirstName = "Test employee",
					Locker = locker1
				};

				context.Lockers.Add(locker1);
				context.Employees.Add(employee);

				project2.LeadCoordinator = manager2;

				context.SaveChanges();
			}
		}

		#endregion

		#region Test Initialize and Cleanup

		[TestInitialize]
		public virtual void CreateTransactionOnTestInitialize()
		{
			_transactionScope = new TransactionScope(TransactionScopeOption.RequiresNew, new TransactionOptions { Timeout = new TimeSpan(0, 10, 0) });
		}

		[TestCleanup]
		public virtual void DisposeTransactionOnTestCleanup()
		{
			Transaction.Current.Rollback();
			_transactionScope.Dispose();
		}

		#endregion

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

		[TestMethod]
		public void DoesNotUpdateEntityIfNoChangesHaveBeenMade()
		{
			Models.Company company1;
			using (var context = new TestDbContext())
			{
				company1 = context.Companies.Single(p => p.Id == 2);
			} // Simulate detach

			using (var context = new TestDbContext())
			{
				context.UpdateGraph(company1, null);
				Assert.IsTrue(context.ChangeTracker.Entries().All(p => p.State == EntityState.Unchanged));
			}
		}

		[TestMethod]
		public void MarksAssociatedRelationAsChangedEvenIfEntitiesAreUnchanged()
		{
			Models.Project project1;
			Models.Manager manager1;
			using (var context = new TestDbContext())
			{
				project1 = context.Projects.Include(m => m.LeadCoordinator).Single(p => p.Id == 1);
				manager1 = context.Managers.First();
			} // Simulate detach

			project1.LeadCoordinator = manager1;

			using (var context = new TestDbContext())
			{
				context.UpdateGraph(project1, p => p.AssociatedEntity(e => e.LeadCoordinator));
				context.SaveChanges();
				Assert.IsTrue(context.Projects.Include(m => m.LeadCoordinator).Single(p => p.Id == 1).LeadCoordinator == manager1);
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
			}

			// Force reload of DB entities.
			// note can also be done with GraphDiffConfiguration.ReloadAssociatedEntitiesWhenAttached.
			using (var context = new TestDbContext())
			{
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

			project.LeadCoordinator = new Models.Manager { Key = "asdfxv", FirstName = "Br", PartKey = "TER", PartKey2 = 2 };

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
			company1.Contacts.First().FirstName = "Bobby"; // change to bobby

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
					.LastName == "Jones");
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

		// added as per ticket #5
		// also tried to add some more complication to this graph to ensure everything works well
		[TestMethod]
		public void OwnedMultipleLevelCollectionMappingWithAssociatedReload()
		{
			Models.MultiLevelTest multiLevelTest;
			Models.Hobby hobby;
			using (var context = new TestDbContext())
			{
				multiLevelTest = context.MultiLevelTest.Add(new Models.MultiLevelTest
				{
					Managers = new[] // test arrays as well
					{
						new Models.Manager 
						{
							Key = "dasfds",
							PartKey = "xxx",
							PartKey2 = 2,
							Employees = new List<Models.Employee>
							{
								new Models.Employee 
								{
									Key = "xsdf",
									FirstName = "Asdf",
									Hobbies = new List<Models.Hobby>
									{
										new Models.Hobby 
										{
											HobbyType = "Test hobby type"
										}
									}
								 }
							 }
						}
					}
				});

				hobby = context.Hobbies.Add(new Models.Hobby { HobbyType = "Skiing" });
				context.SaveChanges();
			} // Simulate detach

			// Graph changes

			// Should not update changes to hobby
			hobby.HobbyType = "Something Else";

			// Update changes to manager
			var manager = multiLevelTest.Managers.First();
			manager.FirstName = "Tester";

			// Update changes to employees
			var employeeToUpdate = manager.Employees.First();
			employeeToUpdate.Hobbies.Clear();
			employeeToUpdate.Hobbies.Add(hobby);
			manager.Employees.Add(new Models.Employee
			{
				FirstName = "Tim",
				Key = "Tim1",
				Manager = multiLevelTest.Managers.First()
			});

			using (var context = new TestDbContext())
			{
				GraphDiffConfiguration.ReloadAssociatedEntitiesWhenAttached = true;
				// Setup mapping
				context.UpdateGraph(multiLevelTest, map => map
					.OwnedCollection(x => x.Managers, withx => withx
						.AssociatedCollection(pro => pro.Projects)
						.OwnedCollection(p => p.Employees, with => with
							.AssociatedCollection(m => m.Hobbies)
							.OwnedEntity(m => m.Locker))));

				context.SaveChanges();

				GraphDiffConfiguration.ReloadAssociatedEntitiesWhenAttached = false;

				var result = context.MultiLevelTest
					.Include("Managers.Employees.Hobbies")
					.Include("Managers.Employees.Locker")
					.Include("Managers.Projects")
					.First();

				var updateManager = result.Managers.Single(p => p.PartKey == manager.PartKey && p.PartKey2 == manager.PartKey2);
				var updateEmployee = updateManager.Employees.Single(p => p.Key == employeeToUpdate.Key);
				var updateHobby = context.Hobbies.Single(p => p.Id == hobby.Id);

				Assert.IsTrue(updateManager.Employees.Count() == 2);
				Assert.IsTrue(result.Managers.First().FirstName == "Tester");
				Assert.IsTrue(updateEmployee.Hobbies.Count() == 1);
				Assert.IsTrue(updateEmployee.Hobbies.First().HobbyType == "Skiing");
				Assert.IsTrue(updateHobby.HobbyType == "Skiing");
				Assert.IsTrue(result.Managers.First().Employees.Any(p => p.Key == "Tim1"));
			}
		}

		#endregion

		#region 2 way relation

		[TestMethod]
		public void EnsureWeCanUseCyclicRelationsOnOwnedCollections()
		{
			Models.Manager manager;
			using (var context = new TestDbContext())
			{
				manager = context.Managers.Include(p => p.Employees).First();
			} // Simulate disconnect

			var newEmployee = new Models.Employee { Key = "assdf", FirstName = "Test Employee", Manager = manager };
			manager.Employees.Add(newEmployee);

			using (var context = new TestDbContext())
			{
				context.UpdateGraph(manager, m1 => m1.OwnedCollection(o => o.Employees));
				context.SaveChanges();
				Assert.IsTrue(context.Employees.Include(p => p.Manager).Single(p => p.Key == "assdf").Manager.FirstName == manager.FirstName);
			}
		}

		#endregion

		#region Child List of items that use a Base class that contain their Id/Key
		[TestMethod]
		public void ModifyListOfItemsWithNonMappedBaseClass()
		{
			int id = 0;
            byte[] rowVersion = null;
			using(var db = new TestDbContext())
			{
				var contact = new Models.Contact
				{
					Name = "Test",
					ContactInfos = new List<Models.ContactContactInfo>
					{
						new Models.ContactContactInfo
						{
							Type = "Phone",
							Value = "1234567890"
						},
						new Models.ContactContactInfo
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

			using(var db = new TestDbContext())
			{
				var contact = new Models.Contact
				{
                    RowVersion = rowVersion,
					Id = id,
					Name = "Test2",
					ContactInfos = new List<Models.ContactContactInfo>
					{
						new Models.ContactContactInfo
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
            int id = 0;
            using (var db = new TestDbContext())
            {
                var item = new Models.NonMappedInheritor
                {
                    FirstName = "Tim"
                };
                db.MappedBase.Add(item);
                db.SaveChanges();
                id = item.Id;
            }

            using (var db = new TestDbContext())
            {
                var item = new Models.NonMappedInheritor
                {
                    Id = id,
                    FirstName = "James"
                };

                db.UpdateGraph(item);
                db.SaveChanges();
                Assert.IsTrue(db.MappedBase.OfType<Models.NonMappedInheritor>().Single(p => p.Id == id).FirstName == "James");
            }
        }

		#endregion

        #region Optimistic Concurrency Tests

        [TestMethod]
        [ExpectedException(typeof(DbUpdateConcurrencyException))]
        public void EditingOutOfDateModelShouldThrowOptimisticConcurrencyException()
        {
            int id = 0;
            byte[] rowVersion = new byte[1];
            using (var db = new TestDbContext())
            {
                var contact = new Models.Contact
                {
                    Name = "Test",
                    ContactInfos = new List<Models.ContactContactInfo>
                    {
                        new Models.ContactContactInfo
                        {
                            Type = "Phone",
                            Value = "1234567890"
                        },
                        new Models.ContactContactInfo
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
                var contact = new Models.Contact
                {
                    RowVersion = new byte[] { 0x0, 0x0, 0x0, 0x0, 0x1, 0x1, 0x1, 0x1},
                    Id = id,
                    Name = "Test2",
                    ContactInfos = new List<Models.ContactContactInfo>
                    {
                        new Models.ContactContactInfo
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

        #endregion

        #region Expressions stored as fields and properties

        [TestMethod]
        public void EnsureWeCanVisitExpressionsStoredAsFields()
        {
            Models.Project project;
            using (var context = new TestDbContext())
            {
                project = context.Projects
                    .Include(p => p.LeadCoordinator)
                    .Single(p => p.Id == 2);

            } // Simulate detach

            project.LeadCoordinator.FirstName = "Tada";

            Expression<Func<Models.Project, Models.Manager>> lambda = (p => p.LeadCoordinator);
            Expression<Func<IUpdateConfiguration<Models.Project>, dynamic>> exp = map => map.OwnedEntity(lambda);

            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(project, exp);

                context.SaveChanges();
                Assert.IsTrue(context.Projects
                    .Include(p => p.LeadCoordinator)
                    .Single(p => p.Id == 2)
                    .LeadCoordinator.FirstName == "Tada");
            }
        }

        public Expression<Func<Models.Project, Models.Manager>> Lambda { get; set; }

        [TestMethod]
        public void EnsureWeCanVisitExpressionsStoredAsProperties()
        {
            Models.Project project;
            using (var context = new TestDbContext())
            {
                project = context.Projects
                    .Include(p => p.LeadCoordinator)
                    .Single(p => p.Id == 2);

            } // Simulate detach

            project.LeadCoordinator.FirstName = "Tada";

            Lambda = (p => p.LeadCoordinator);
            Expression<Func<IUpdateConfiguration<Models.Project>, dynamic>> exp = map => map.OwnedEntity(Lambda, with => with.OwnedCollection(m => m.Hobbies));

            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(project, exp);

                context.SaveChanges();
                Assert.IsTrue(context.Projects
                    .Include(p => p.LeadCoordinator)
                    .Single(p => p.Id == 2)
                    .LeadCoordinator.FirstName == "Tada");
            }
        }


        #endregion

        #region Attach new root

        [TestMethod]
        public void EnsureNewRootsCanBeAdded()
        {
            var manager = new Manager
            {
                Key = "Some",
                PartKey = "Boss",
                PartKey2 = 23,
                Employees = new Collection<Employee>()
            };
            var newEmployee = new Employee { Key = "SomeOther", FirstName = "Employee", Manager = manager };
            manager.Employees.Add(newEmployee);

            using (var context = new TestDbContext())
            {
                context.UpdateGraph(manager, m => m.OwnedCollection(n => n.Employees));
                context.SaveChanges();

                var employee = context.Employees.Include(e => e.Manager).Single(e => e.Key == "SomeOther");
                Assert.IsTrue(employee.Manager.Key == manager.Key);
            }
        }

        #endregion
    }
}
