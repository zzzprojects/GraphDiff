/*
 * This code is provided as is with no warranty. If you find a bug please report it on github.
 * If you would like to use the code please leave this comment at the top of the page
 * License MIT (c) Brent McKendrick 2012
 */

namespace RefactorThis.GraphDiff
{
    /// <summary>The mode used when querying for an entity graph</summary>
    public enum QueryMode
    {
        /// <summary>Perform one database query to load the entity graph</summary>
        SingleQuery,

        /// <summary>Perform multiple database queries to load the entity graph (sometimes more performant for complex graphs)</summary>
        MultipleQuery
    }
}
