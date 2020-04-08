using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using RefactorThis.GraphDiff;

namespace Z.EntityFrameworkGraphDiff.labEF6
{
	public class Template
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

			var entity = new EntitySimple {ColumnInt = 0};
			// SEED  
			using (var context = new EntityContext())
			{
				context.EntitySimples.Add(entity);

				context.SaveChanges();
			}

			entity.EntitySimpleChild = new EntitySimpleChild(){ ColumnInt =  1};
			entity.EntitySimpleLists = new List<EntitySimpleList>() {new EntitySimpleList() {ColumnInt =  2}, new EntitySimpleList() { ColumnInt = 3 } };

			// TEST  
			using (var context = new EntityContext())
			{
				context.UpdateGraph(entity, map => map.AssociatedEntity(c => c.EntitySimpleChild).AssociatedCollection(c => c.EntitySimpleLists));
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
				base.OnModelCreating(modelBuilder);
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
		}
		public class EntitySimpleList
		{
			public int ID { get; set; }
			public int ColumnInt { get; set; }
			public String ColumnString { get; set; }
		}
	}
}