using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;


namespace RefactorThis.GraphDiff
{
	public static class ReflectionHelper
	{
		// Returns the types in the class chain for this type, starting with the type passed in, up to
		// the last base class before object.
		public static IEnumerable<Type> GetBaseTypes(Type type)
		{
			yield return type;

			Type baseType = type;
			while (baseType.BaseType != null &&
				   baseType.BaseType != typeof(object))
			{
				baseType = baseType.BaseType;
				yield return baseType;
			}
		}

		// Returns the types in the class chain from the top down, starting with the type below object.
		public static IEnumerable<Type> GetBaseTypesDescending(Type type)
		{
			return GetBaseTypes(type).Reverse();
		}
	}
}
