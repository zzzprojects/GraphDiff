using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace RefactorThis.GraphDiff.Tests.Models
{
    // Manage your company, contacts and projects.

    public class Company
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public virtual ICollection<CompanyContact> Contacts { get; set; }
    }

    public class CompanyContact
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public virtual ICollection<ContactInfo> Infos { get; set; }
    }

    public class ContactInfo
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
    }

    public class Project
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime Deadline { get; set; }
        public Manager LeadCoordinator { get; set; }
        public virtual ICollection<Company> Stakeholders { get; set; }
    }

    public class MultiLevelTest
    {
        public int Id { get; set; }
        public ICollection<Manager> Managers { get; set; }
    }

    public class Manager
    {
        // to allow for testing of multi keys and data annotations
        [Key]
        [Column(Order=1)]
        public string PartKey { get; set; }
        [Key]
        [Column(Order = 2)]
        public int PartKey2 { get; set; }

        public string FirstName { get; set; }
        public ICollection<Project> Projects { get; set; }
        public virtual ICollection<Employee> Employees { get; set; }
    }

    public class Employee
    {
        // This key will be configured in fluent api
        public string Key { get; set; }
        public virtual Manager Manager { get; set; } // cyclic navigation
        public string FirstName { get; set; }
        public Locker Locker { get; set; }
        public ICollection<Hobby> Hobbies { get; set; }
    }

    public class Locker
    {
        public int Id { get; set; }
        public string Combination { get; set; }
        public string Location { get; set; }
    }

    public class Hobby
    {
        public int Id { get; set; }
        public string HobbyType { get; set; }
        public ICollection<Employee> Employees { get; set; }
    }
}
