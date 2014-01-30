using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RefactorThis.GraphDiff.Tests.Models;

namespace RefactorThis.GraphDiff.Tests.Tests
{
    [TestClass]
    public class MiscBehaviours : TestBase
    {
        [TestMethod]
        public void ShouldSupportMultipleKeys()
        {
            var model = new MultiKeyModel
            {
                Title = "Hello",
                Date = DateTime.Now,
                KeyPart1 = "A123",
                KeyPart2 = "A234"
            };

            using (var context = new TestDbContext())
            {
                context.MultiKeyModels.Add(model);
                context.SaveChanges();
            } // simulate detach

            model.Date = DateTime.Parse("01/01/2010");
            using (var context = new TestDbContext())
            {
                model = context.UpdateGraph(model);
                context.SaveChanges();

                context.Entry(model).Reload();
                Assert.IsTrue(model.Date == DateTime.Parse("01/01/2010"));
            }
        }

        [TestMethod]
        public void ShouldSupportInternalKeys()
        {
            using (var context = new TestDbContext())
            {
                var model = context.UpdateGraph(new InternalKeyModel());
                context.SaveChanges();

                Assert.AreNotEqual(0, model.Id);
            }
        }
    }
}
