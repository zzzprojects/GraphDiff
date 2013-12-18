using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Data.Entity;
using System.Reflection;


namespace RefactorThis.GraphDiff
{
	/// <summary>
	/// Provides a HashSet of the Entity Types mapped in any given DbContext as DbSet/IDbSets.
	/// Provides basic caching, so a given DbContext is only mapped once.
	/// 
	/// Thread-safe.
	/// </summary>
	public class DbContextTypes
	{
		#region Singleton
		// http://codereview.stackexchange.com/questions/79/implementing-a-singleton-pattern-in-c
		public static DbContextTypes Current { get { return Nested.instance; } }

		class Nested
		{
			static Nested()
			{
			}

			internal static readonly DbContextTypes instance = new DbContextTypes();
		}
		#endregion

		protected ConcurrentDictionary<Type, ImmutableHashSet<Type>> dbSetTypeToEntityTypes = new ConcurrentDictionary<Type,ImmutableHashSet<Type>>();

		/// <summary>
		/// Returns a HashSet of the Entity Types mapped in the passed DbContext.
		/// </summary>
		/// <param name="db"></param>
		/// <returns></returns>
		public ImmutableHashSet<Type> EntityTypesFor(DbContext db)
		{
			var type = db.GetType();
			if (dbSetTypeToEntityTypes.ContainsKey(type))
				return dbSetTypeToEntityTypes[type];

			var dbSetTypes = ImmutableHashSet.Create(db.GetType()
				.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
				// TODO: Filter to IDbSet/DbSet just in case there are other weirdo properties declared on the DbContext
				//.Where(p => typeof(DbSet).IsAssignableFrom(p.PropertyType))
				.Select(p => p.PropertyType.GetGenericArguments()[0])
				.ToArray());

			// Either add what we generated, or if other code was also generating the same thing and got there first, throw ours away
			dbSetTypeToEntityTypes.TryAdd(type, dbSetTypes);
			return dbSetTypes;
		}
	}
}
