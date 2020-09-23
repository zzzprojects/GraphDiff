/*
 * This code is provided as is with no warranty. If you find a bug please report it on github.
 * If you would like to use the code please leave this comment at the top of the page
 * License MIT (c) Brent McKendrick 2012
 */

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace RefactorThis.GraphDiff
{
    /// <summary>
    /// Mapping configuration for a merge graph
    /// </summary>
    /// <typeparam name="T">The type of the parent entity</typeparam>
    public interface IUpdateConfiguration<T> { }

    public static class UpdateConfigurationExtensions
    {
        /// <summary>
        /// States that the child entity is a part of the aggregate and will be updated, added or removed if changed in the parent's
        /// navigational property.
        /// </summary>
        /// <typeparam name="T">The parent entity type</typeparam>
        /// <typeparam name="T2">The child entity type </typeparam>
        /// <param name="config">The configuration mapping</param>
        /// <param name="expression">An expression specifying the right hand side entity</param>
        /// <returns>Updated configuration mapping</returns>
        public static IUpdateConfiguration<T> OwnedEntity<T, T2>(this IUpdateConfiguration<T> config, Expression<Func<T, T2>> expression)
        {
            return config;
        }

        /// <summary>
        /// States that the child entity is not a part of the aggregate. The parent's navigation property will be updated, but changes to the
        /// child will not be saved.
        /// </summary>
        /// <typeparam name="T">The parent entity type</typeparam>
        /// <typeparam name="T2">The child entity type </typeparam>
        /// <param name="config">The configuration mapping</param>
        /// <param name="expression">An expression specifying the child entity</param>
        /// <returns>Updated configuration mapping</returns>
        public static IUpdateConfiguration<T> AssociatedEntity<T, T2>(this IUpdateConfiguration<T> config, Expression<Func<T, T2>> expression)
        {
            return config;
        }

        /// <summary>
        /// States that the child entity is a part of the aggregate and will be updated, added or removed if changed in the parent's
        /// navigational property.
        /// </summary>
        /// <typeparam name="T">The parent entity type</typeparam>
        /// <typeparam name="T2">The child entity type </typeparam>
        /// <param name="config">The configuration mapping</param>
        /// <param name="expression">An expression specifying the right hand side entity</param>
        /// <param name="mapping">Any further mapping for the children of this relation</param>
        /// <returns>Updated configuration mapping</returns>
        public static IUpdateConfiguration<T> OwnedEntity<T, T2>(this IUpdateConfiguration<T> config, Expression<Func<T, T2>> expression, Expression<Func<IUpdateConfiguration<T2>, object>> mapping)
        {
            return config;
        }

        /// <summary>
        /// States that the child collection is a part of the aggregate and the entities inside will be updated,
        /// added or removed if changed in the parent's navigational property.
        /// </summary>
        /// <typeparam name="T">The parent entity type</typeparam>
        /// <typeparam name="T2">The child collection type </typeparam>
        /// <param name="config">The configuration mapping</param>
        /// <param name="expression">An expression specifying the right hand side entity</param>
        /// <returns>Updated configuration mapping</returns>
        public static IUpdateConfiguration<T> OwnedCollection<T, T2>(this IUpdateConfiguration<T> config, Expression<Func<T, ICollection<T2>>> expression)
        {
            return config;
        }

        /// <summary>
        /// States that the child collection is not a part of the aggregate. The parent's navigation property will be updated, but entity changes to the
        /// child entities will not be saved.
        /// </summary>
        /// <typeparam name="T">The parent entity type</typeparam>
        /// <typeparam name="T2">The child entity type </typeparam>
        /// <param name="config">The configuration mapping</param>
        /// <param name="expression">An expression specifying the child entity</param>
        /// <returns>Updated configuration mapping</returns>
        public static IUpdateConfiguration<T> AssociatedCollection<T, T2>(this IUpdateConfiguration<T> config, Expression<Func<T, ICollection<T2>>> expression)
        {
            return config;
        }

        /// <summary>
        /// States that the child collection is not a part of the aggregate. The parent's navigation property will be updated, but entity changes to the
        /// child entities will not be saved.
        /// </summary>
        /// <typeparam name="T">The parent entity type</typeparam>
        /// <typeparam name="T2">The child entity type </typeparam>
        /// <param name="config">The configuration mapping</param>
        /// <param name="expression">An expression specifying the child entity</param>
        /// <param name="navExpression">An navigation expression specifying the parent entity</param>
        /// <returns>Updated configuration mapping</returns>
        public static IUpdateConfiguration<T> AssociatedCollection<T, T2>(this IUpdateConfiguration<T> config, Expression<Func<T, ICollection<T2>>> expression, Expression<Func<T2, T>> navExpression)
        {
            return config;
        }

        /// <summary>
        /// States that the child collection is a part of the aggregate and the entities inside will be updated,
        /// added or removed if changed in the parent's navigational property.
        /// </summary>
        /// <typeparam name="T">The parent entity type</typeparam>
        /// <typeparam name="T2">The child collection type </typeparam>
        /// <param name="config">The configuration mapping</param>
        /// <param name="expression">An expression specifying the right hand side entity</param>
        /// <param name="mapping">Any further mapping for the children of this relation</param>
        /// <returns>Updated configuration mapping</returns>
        public static IUpdateConfiguration<T> OwnedCollection<T, T2>(this IUpdateConfiguration<T> config, Expression<Func<T, ICollection<T2>>> expression, Expression<Func<IUpdateConfiguration<T2>, object>> mapping)
        {
            return config;
        }
    }
}
