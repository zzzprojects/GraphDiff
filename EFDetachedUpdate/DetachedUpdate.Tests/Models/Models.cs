using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace RefactorThis.DetachedUpdate.Tests.Models
{
    // Manage your company, contacts and projects.

    public class Company
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public List<CompanyContact> Contacts { get; set; }
    }

    public class CompanyContact
    {
        [Key]
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public List<ContactInfo> Infos { get; set; }
    }

    public class ContactInfo
    {
        [Key]
        public int Id { get; set; }
        public string Description { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
    }

    public class Project
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime Deadline { get; set; }
        public Manager LeadCoordinator { get; set; }
        public List<Company> Stakeholders { get; set; }
    }

    public class Manager
    {
        // to allow for testing of multi keys
        [Key]
        [Column(Order=1)]
        public string PartKey { get; set; }
        [Key]
        [Column(Order = 2)]
        public int PartKey2 { get; set; }

        public string FirstName { get; set; }
        public List<Employee> Employees { get; set; }
    }

    public class Employee
    {
        [Key]
        public string Key { get; set; }
        public string FirstName { get; set; }
    }

}
