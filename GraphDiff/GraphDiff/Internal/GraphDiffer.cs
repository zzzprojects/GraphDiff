using RefactorThis.GraphDiff.Internal.Graph;
using System;
using System.Collections.Generic;
using System.Data.Entity;

namespace RefactorThis.GraphDiff.Internal
{
    internal interface IGraphDiffer<T> where T : class, new()
    {
        T Merge(T updating, QueryMode queryMode = QueryMode.SingleQuery);
    }

    /// <summary>
    /// GraphDiff main entry point.
    /// </summary>
    /// <typeparam name="T">The root agreggate type</typeparam>
    internal class GraphDiffer<T> : IGraphDiffer<T> where T : class, new()
    {
        private readonly GraphNode _root;
        private readonly DbContext _dbContext;
        private readonly IQueryLoader _queryLoader;
        private readonly IEntityManager _entityManager;

        public GraphDiffer(DbContext dbContext, IQueryLoader queryLoader, IEntityManager entityManager, GraphNode root)
        {
            _root = root;
            _dbContext = dbContext;
            _queryLoader = queryLoader;
            _entityManager = entityManager;
        }

        public T Merge(T updating, QueryMode queryMode = QueryMode.SingleQuery)
        {
            // todo query mode
            bool isAutoDetectEnabled = _dbContext.Configuration.AutoDetectChangesEnabled;
            try
            {
                // performance improvement for large graphs
                _dbContext.Configuration.AutoDetectChangesEnabled = false;

                // Get our entity with all includes needed, or add a new entity
                var includeStrings = _root.GetIncludeStrings(_entityManager);
                T persisted = _queryLoader.LoadEntity(updating, includeStrings, queryMode);

                if (persisted == null)
                {
                    // we are always working with 2 graphs, simply add a 'persisted' one if none exists,
                    // this ensures that only the changes we make within the bounds of the mapping are attempted.
                    persisted = new T();
                    _dbContext.Set<T>().Add(persisted);
                }

                if (_dbContext.Entry(updating).State != EntityState.Detached)
                {
                    throw new InvalidOperationException("GraphDiff supports detached entities only at this time. Please try AsNoTracking() or detach your entites before calling the UpdateGraph method");
                }

                // Perform recursive update
                var entityManager = new EntityManager(_dbContext);
                var changeTracker = new ChangeTracker(_dbContext, entityManager);
                _root.Update(changeTracker, entityManager, persisted, updating);

                return persisted;
            }
            finally
            {
                _dbContext.Configuration.AutoDetectChangesEnabled = isAutoDetectEnabled;
            }
        }
    }
}