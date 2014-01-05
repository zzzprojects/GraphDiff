using System;
using System.Collections.Generic;

namespace RefactorThis.GraphDiff.Tests
{
    public static class DatabaseScript
    {
        public static void Run()
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

                context.Projects.Add(new Models.Project
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
    }
}
