/*
 * This code is provided as is with no warranty. If you find a bug please report it on github.
 * If you would like to use the code please leave this comment at the top of the page
 * License MIT (c) Brent McKendrick 2012
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace RefactorThis.GraphDiff
{
    /// <summary>
    /// Configuration settings for a single update query. These will override any global defaults.
    /// </summary>
    /// <typeparam name="T">Type of entity the update operation is working with</typeparam>
    public class UpdateParams<T>
    {
        public QueryMode QueryMode { get; set; }
        public string MappingScheme { get; set; }
        public Expression<Func<IUpdateConfiguration<T>, object>> Mapping { get; set; }
    }
}
