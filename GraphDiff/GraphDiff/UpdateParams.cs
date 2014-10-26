/*
 * This code is provided as is with no warranty. If you find a bug please report it on github.
 * If you would like to use the code please leave this comment at the top of the page
 * License MIT (c) Brent McKendrick 2012
 */

namespace RefactorThis.GraphDiff
{
    /// <summary>Configuration settings for a single update query. These will override any global defaults.</summary>
    public class UpdateParams
    {
        /// <summary>Mode of querying</summary>
        public QueryMode QueryMode { get; set; }
    }
}
