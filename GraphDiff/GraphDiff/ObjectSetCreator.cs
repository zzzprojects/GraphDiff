using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;


namespace RefactorThis.GraphDiff
{
	/// <summary>
	/// Determines the best ObjectSet type for a given Entity Type, and caches the method for creating one.
	/// Uses cached method to create ObjectSets for passed DbContexts.
	/// 
	/// Thread-safe.
	/// </summary>
	public class ObjectSetCreator
	{
		#region Singleton
		// http://codereview.stackexchange.com/questions/79/implementing-a-singleton-pattern-in-c
		public static ObjectSetCreator Current { get { return Nested.instance; } }

		class Nested
		{
			static Nested()
			{
			}

			internal static readonly ObjectSetCreator instance = new ObjectSetCreator();
		}
		#endregion

		protected ConcurrentDictionary<Type, MethodInfo> objectSetCreators = new ConcurrentDictionary<Type, MethodInfo>();

		public ObjectContext ObjectContextFor(DbContext db)
		{
			return ((System.Data.Entity.Infrastructure.IObjectContextAdapter)db).ObjectContext;
		}

		protected MethodInfo getCreateObjectSetFor(Type entityType, DbContext db)
		{
			var dbSetTypes = DbContextTypes.Current.EntityTypesFor(db);

			var objectSetEntityType = ReflectionHelper.GetBaseTypesDescending(entityType).FirstOrDefault(t => dbSetTypes.Contains(t));

			if (objectSetEntityType == null)
			{
				//throw new MissingFieldException("No DbSet exists in the DbContext for type " + entityType.Name + " or any of its base classes.");
				// Just blunder forward with the original type and hope
				objectSetEntityType = entityType;
			}

			var objectContext = ObjectContextFor(db);
			MethodInfo objectContext_CreateObjectSet = objectContext.GetType().GetMethod("CreateObjectSet", new Type[] { })
														.MakeGenericMethod(objectSetEntityType);

			return objectContext_CreateObjectSet;
		}

		protected MethodInfo GetCreateObjectSetFor(Type entityType, DbContext db)
		{
			if (objectSetCreators.ContainsKey(entityType))
				return objectSetCreators[entityType];

			var creator = getCreateObjectSetFor(entityType, db);
			objectSetCreators.TryAdd(entityType, creator);
			return creator;
		}

		/// <summary>
		/// Searches down the Entity's base chain until it finds a valid ObjectSet.
		/// Throws an Exception if none found.
		/// </summary>
		/// <param name="entityType"></param>
		/// <param name="db"></param>
		/// <returns></returns>
		public object CreateObjectSetFor(Type entityType, DbContext db)
		{
			var objectContext_CreateObjectSet = GetCreateObjectSetFor(entityType, db);
			var objectContext = ObjectContextFor(db);
			// May throw
			object set = objectContext_CreateObjectSet.Invoke(objectContext, null);
			return set;
		}
	}
}
