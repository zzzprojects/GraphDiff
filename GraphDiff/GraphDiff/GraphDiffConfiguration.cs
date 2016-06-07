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
        /// If an entity has integer primary keys (int, uint, long, ulong) with
        /// empty values, it is considered to be new and not persisted.
        /// In this case the loading of a persisted version of this entity can
        /// be skipped to increase the performance of inserts.
        /// </summary>
        public static bool SkipLoadingOfNewEntities { get; set; }
    }
}