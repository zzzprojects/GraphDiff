using System.Data.Entity;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RefactorThis.GraphDiff.Tests
{
    [TestClass]
    public class Bootstrapper
    {
        [AssemblyInitialize]
        public static void Initialize(TestContext context)
        {
            Database.SetInitializer(new DropCreateDatabaseAlways<TestDbContext>());
            DatabaseScript.Run();
        }
    }
}
