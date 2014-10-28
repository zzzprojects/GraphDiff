/*
 * This code is provided as is with no warranty. If you find a bug please report it on github.
 * If you would like to use the code please leave this comment at the top of the page
 * License MIT (c) Brent McKendrick 2012
 */

namespace RefactorThis.GraphDiff
{
    /// <summary>
    /// Static configuration class for GraphDiff options
    /// </summary>
    public static class GraphDiffConfiguration
    {
        /// <summary>
        /// If an entity is attached as an associated entity it will be automatically reloaded from the database
        /// to ensure the EF local cache has the latest state.
        /// </summary>
        public static bool ReloadAssociatedEntitiesWhenAttached { get; set; }

        /// <summary>
        /// If GraphDiff encounters an entity in a database collection that is no longer part of the graph it is 
        /// currently attaching, it will remove that element from the database as well unless this flag is set.
        /// </summary>
        public static bool NeverRemoveFromCollections { get; set; }
    }
}
