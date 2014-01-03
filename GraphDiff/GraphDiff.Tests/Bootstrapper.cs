using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.Entity;

namespace RefactorThis.GraphDiff.Tests
{
    [TestClass]
    public class Bootstrapper
    {
        [AssemblyInitialize]
        public static void Initialize(TestContext context)
        {
            Database.SetInitializer<TestDbContext>(new DropCreateDatabaseAlways<TestDbContext>());
            DatabaseScript.Run();
        }
    }
}
