using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
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

        [TestMethod]
        public void ShouldSupportInternalNavigationProperties()
        {
            var parent = new InternalKeyModel();
            using (var context = new TestDbContext())
            {
                context.InternalKeyModels.Add(parent);
                context.SaveChanges();
            } // simulate detach

            parent.Associates = new List<InternalKeyAssociate> { new InternalKeyAssociate() };

            InternalKeyModel model;
            using (var context = new TestDbContext())
            {
                model = context.UpdateGraph(parent, map => map.AssociatedCollection(ikm => ikm.Associates));
                context.SaveChanges();

                Assert.AreNotEqual(0, model.Id);

                Assert.IsNotNull(model.Associates);
                Assert.AreEqual(1, model.Associates.Count);
                Assert.AreNotEqual(0, model.Associates.First().Id);
            }

            using (var context = new TestDbContext())
            {
                var reloadedModel = context.InternalKeyModels
                        .Include(ikm => ikm.Associates)
                        .SingleOrDefault(ikm => ikm.Id == model.Id);

                Assert.IsNotNull(reloadedModel);
                Assert.IsNotNull(reloadedModel.Associates);

                Assert.AreEqual(1, reloadedModel.Associates.Count);
                Assert.AreEqual(model.Associates.Single().Id, reloadedModel.Associates.Single().Id);
            }
        }

        [TestMethod]
        public void ShouldSupportNullableKeys()
        {
            using (var context = new TestDbContext())
                context.Database.ExecuteSqlCommand("ALTER TABLE NullableKeyModels ALTER COLUMN Id uniqueidentifier NOT NULL");

            NullableKeyModel model = new NullableKeyModel();
            using (var context = new TestDbContext())
            {
                context.NullableKeyModels.Add(model);
                context.SaveChanges();
            }

            using (var context = new TestDbContext())
            {
                model = context.UpdateGraph(model);
                context.SaveChanges();

                Assert.AreNotEqual(0, model.Id);
            }
        }
    }
}
