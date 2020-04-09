using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using RefactorThis.GraphDiff;

namespace Z.EntityFrameworkGraphDiff.labEF6
{
	public class Request_LazyLoad
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
				entity.EntitySimpleChild = new EntitySimpleChild() { ColumnInt = 1 };
				 var EntitySimpleLists = new List<EntitySimpleList>() { new EntitySimpleList() { ColumnInt = 2 }, new EntitySimpleList() { ColumnInt = 3 } };

				context.EntitySimples.Add(entity);

				context.EntitySimpleLists.AddRange(EntitySimpleLists);
				context.SaveChanges();
			}

			EntitySimpleList entityChildList = null;
			using (var context = new EntityContext())
			{
				entity = context.EntitySimples.Include(x => x.EntitySimpleChild).First();
				 entityChildList = context.EntitySimpleLists.First();
				 entity.EntitySimpleLists = new List<EntitySimpleList>() { entityChildList };
			}

			entityChildList.ColumnInt = 20;
			entityChildList.EntitySimpleList2s = new List<EntitySimpleList2>() {new EntitySimpleList2()};
			
			// TEST  
			using (var context = new EntityContext())
			{
				
				 context.UpdateGraph(entity, map => map.AssociatedEntity(c => entity.EntitySimpleLists).AssociatedCollection(c => c.EntitySimpleLists));
				 //context.UpdateGraph(entity.EntitySimpleLists.First(), map => map.AssociatedEntity(c => c.EntitySimpleList2s));
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
			public DbSet<EntitySimpleList2> EntitySimpleList2s { get; set; }

			protected override void OnModelCreating(DbModelBuilder modelBuilder)
			{
				base.OnModelCreating(modelBuilder);
				Configuration.LazyLoadingEnabled = true;
				Configuration.ProxyCreationEnabled = true;
			}
		}

		public class EntitySimple
		{
			public int ID { get; set; }
			public int ColumnInt { get; set; }
			public String ColumnString { get; set; }

			public virtual EntitySimpleChild EntitySimpleChild { get; set; }
			public virtual ICollection<EntitySimpleList> EntitySimpleLists { get; set; }
		}


		public class EntitySimpleChild
		{
			public int ID { get; set; }
			public int ColumnInt { get; set; }
			public String ColumnString { get; set; }
			public virtual ICollection<EntitySimpleList2> EntitySimpleList2s { get; set; }
		}
		public class EntitySimpleList
		{
			public int ID { get; set; }
			public int ColumnInt { get; set; }
			public String ColumnString { get; set; }
			public virtual ICollection<EntitySimpleList2> EntitySimpleList2s { get; set; }
		}

		public class EntitySimpleList2
		{
			public int ID { get; set; }
			public int ColumnInt { get; set; }
			public String ColumnString { get; set; }
		}
	}
}