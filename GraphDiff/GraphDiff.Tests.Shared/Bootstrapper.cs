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
            using (var db = new TestDbContext())
            {
                if (db.Database.Exists())
                {
                    db.Database.Delete();
                }
                db.Database.Create();
            }
        }
    }
}
