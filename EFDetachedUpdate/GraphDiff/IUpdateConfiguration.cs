/*
 * This code is provided as is with no warranty. If you find a bug please report it on github.
 * If you would like to use the code please leave this comment at the top of the page
 * (c) Brent McKendrick 2012
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace RefactorThis.GraphDiff
{
    /* Configuration and Mapping of update */

    public interface IUpdateConfiguration<T>
    {
    }

    public static class UpdateConfigurationExtensions
    {
        /// <summary>
        /// States that a single entity relation is owned by its parent
        /// </summary>
        public static IUpdateConfiguration<T> OwnedEntity<T, T2>(this IUpdateConfiguration<T> config, Expression<Func<T, T2>> expression)
        {                                    
            return config;                   
        }

        /// <summary>
        /// States that a single entity relation is not owned but associated to its parent
        /// </summary>
        public static IUpdateConfiguration<T> AssociatedEntity<T, T2>(this IUpdateConfiguration<T> config, Expression<Func<T, T2>> expression)
        {
            return config;
        }

        /// <summary>
        /// States that a single entity relation is owned by its parent
        /// </summary>
        /// <param name="expression">The expression which represents the entity relation</param>
        /// <param name="mapping">Any further mapping for the children of this relation</param>
        public static IUpdateConfiguration<T> OwnedEntity<T, T2>(this IUpdateConfiguration<T> config, Expression<Func<T, T2>> expression, Expression<Func<IUpdateConfiguration<T2>, object>> mapping)
        {
            return config;
        }
                                             
        /* Collection configuration */

        /// <summary>
        /// States that a collection and therefore its values are owned by the parent
        /// </summary>
        /// <param name="expression">The expression which represents the collection relation</param>
        public static IUpdateConfiguration<T> OwnedCollection<T, T2>(this IUpdateConfiguration<T> config, Expression<Func<T, ICollection<T2>>> expression)
        {                                  
            return config;                 
        }

        /// <summary>
        /// States that a collection is only an association and the parent does not own the collection
        /// </summary>
        /// <param name="expression">The expression which represents the collection relation</param>
        public static IUpdateConfiguration<T> AssociatedCollection<T, T2>(this IUpdateConfiguration<T> config, Expression<Func<T, ICollection<T2>>> expression)
        {
            return config;
        }

        /// <summary>
        /// States that a collection and therefore its values are owned by the parent
        /// </summary>
        /// <param name="expression">The expression which represents the collection relation</param>
        /// <param name="mapping">Any further mapping for the children of this relation</param>
        public static IUpdateConfiguration<T> OwnedCollection<T, T2>(this IUpdateConfiguration<T> config, Expression<Func<T, ICollection<T2>>> expression, Expression<Func<IUpdateConfiguration<T2>, object>> mapping)
        {                                
            return config;               
        }                                
    }
}
