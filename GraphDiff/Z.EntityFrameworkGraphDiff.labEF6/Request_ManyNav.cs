using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using RefactorThis.GraphDiff;
using System;
using System.Data.Entity; 
using System.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Z.EntityFrameworkGraphDiff.labEF6
{
	class Request_ManyNav
	{
		public static void Execute()
		{
			// Create BD 
			using (var context = new EntityContext())
			{
				My.CreateBD(context);
			}

			// CLEAN  
			using (var context = new EntityContext())
			{
				context.EntitySimples.RemoveRange(context.EntitySimples);
				context.EntitySimpleChilds.RemoveRange(context.EntitySimpleChilds);
				context.EntitySimpleLists.RemoveRange(context.EntitySimpleLists);

				context.SaveChanges();
			}

			var entity = new EntitySimple { ColumnInt = 0 };
			// SEED  
			using (var context = new EntityContext())
			{
				context.EntitySimples.Add(entity);

				context.SaveChanges();
			}

			entity.EntitySimpleChild = new EntitySimpleChild() { ColumnInt = 1 };
			entity.EntitySimpleLists = new List<EntitySimpleList>() { new EntitySimpleList() { ColumnInt = 2 }, new EntitySimpleList() { ColumnInt = 3 } };

			// TEST  
			using (var context = new EntityContext())
			{
				//	var node1 = context.UpdateGraph(entity, map => map.OwnedEntity(p => p.EntitySimpleChild, (Expression<Func<IUpdateConfiguration<EntitySimpleChild>, object>>) with =>
				//		with.OwnedCollection(p => p.EntitySimples)));
				//	context.UpdateGraph(entity, map => map.AssociatedEntity(c => c.EntitySimpleChild).AssociatedCollection(c => c.EntitySimpleLists, x => x.B));

				context.UpdateGraph(entity, map => map.AssociatedCollection(c => c.EntitySimpleLists, x => x.B).AssociatedEntity(c => c.EntitySimpleChild));
			
				//context.UpdateGraph(entity, map => map.AssociatedEntity(c => c.EntitySimpleChild).AssociatedCollection(c => c.EntitySimpleLists));
				//context.UpdateGraph(entity, map => map.OwnedEntity(c => c.EntitySimpleChild).OwnedCollection(c => c.EntitySimpleLists));
				context.SaveChanges();
			}

			entity = new EntitySimple { ColumnInt = 101 };
			using (var context = new EntityContext())
			{
				entity.EntitySimpleChild = new EntitySimpleChild() { ColumnInt = 1 };
				entity.EntitySimpleLists = new List<EntitySimpleList>() { new EntitySimpleList() { ColumnInt = 2 }, new EntitySimpleList() { ColumnInt = 3 } };


				context.EntitySimples.Add(entity);

				context.SaveChanges();
			}

			using (var context = new EntityContext())
			{
				var a = context.EntitySimples.ToList();
				var b = context.EntitySimpleChilds.ToList();
				var c = context.EntitySimpleLists.ToList();
			}
		}

		public class EntityContext : DbContext
		{
			public EntityContext() : base(My.ConnectionString)
			{
			}

			public DbSet<EntitySimple> EntitySimples { get; set; }
			public DbSet<EntitySimpleChild> EntitySimpleChilds { get; set; }
			public DbSet<EntitySimpleList> EntitySimpleLists { get; set; }

			protected override void OnModelCreating(DbModelBuilder modelBuilder)
			{
				modelBuilder.Entity<EntitySimple>()
							.HasOptional(x => x.EntitySimpleChild)
							.WithRequired(y => y.EntitySimple);
				modelBuilder.Entity<EntitySimple>()
							.HasMany<EntitySimpleList>(x => x.EntitySimpleLists)
							.WithRequired(y => y.EntitySimple);
				base.OnModelCreating(modelBuilder);
			}

			internal void UpdateGraph(EntitySimple entity, Func<object, object> p)
			{
				throw new NotImplementedException();
			}
		}

		public class EntitySimple
		{
			public int ID { get; set; }
			public int ColumnInt { get; set; }
			public String ColumnString { get; set; }

			public EntitySimpleChild EntitySimpleChild { get; set; }
			public List<EntitySimpleList> EntitySimpleLists { get; set; }
		}


		public class EntitySimpleChild
		{
			public int ID { get; set; }
			public int ColumnInt { get; set; }
			public String ColumnString { get; set; }

			public EntitySimple A { get; set; }
			public EntitySimple B { get; set; }
			public EntitySimple EntitySimple { get; set; }
		}
		public class EntitySimpleList
		{
			public int ID { get; set; }
			public int ColumnInt { get; set; }
			public String ColumnString { get; set; }

			public EntitySimple A { get; set; } 
			public EntitySimple B { get; set; }
			public EntitySimple EntitySimple { get; set; }
		}
	}
}