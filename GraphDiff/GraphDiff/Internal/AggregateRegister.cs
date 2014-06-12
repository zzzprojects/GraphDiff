using RefactorThis.GraphDiff.Internal.Caching;
using RefactorThis.GraphDiff.Internal.Graph;
using RefactorThis.GraphDiff.Internal.GraphBuilders;
using System;
using System.Linq.Expressions;

namespace RefactorThis.GraphDiff.Internal
{
    internal interface IAggregateRegister
    {
        void ClearAll();
        void Register<T>(GraphNode rootNode, string scheme = null);
        GraphNode GetEntityGraph<T>(string scheme = null);
        GraphNode GetEntityGraph<T>(Expression<Func<IUpdateConfiguration<T>, object>> expression);
    }

    internal class AggregateRegister : IAggregateRegister
    {
        private readonly ICacheProvider _cache;
        private readonly IAttributeGraphBuilder _attributeGraphBuilder;

        public AggregateRegister(ICacheProvider cache)
        {
            _cache = cache;
            _attributeGraphBuilder = new AttributeGraphBuilder();
        }

        public void ClearAll()
        {
            _cache.Clear(typeof(AggregateRegister).FullName);
        }

        public void Register<T>(GraphNode rootNode, string scheme = null)
        {
            _cache.Insert(typeof(AggregateRegister).FullName, GenerateCacheKey<T>(scheme), rootNode);
        }

        public GraphNode GetEntityGraph<T>(string scheme = null)
        {
            return _cache.GetOrAdd<GraphNode>(typeof(AggregateRegister).FullName, GenerateCacheKey<T>(scheme), () =>
            {
                // no cached mapping lets look for attributes
                if (_attributeGraphBuilder.CanBuild(typeof(T)))
                {
                    return _attributeGraphBuilder.BuildGraph<T>();
                }
                else
                {
                    // no mapping by default
                    return new GraphNode();
                }
            });
        }

        public GraphNode GetEntityGraph<T>(Expression<Func<IUpdateConfiguration<T>, object>> expression)
        {
            var key = typeof(T).FullName + "_" + expression.ToString();
            return _cache.GetOrAdd<GraphNode>(typeof(AggregateRegister).FullName, key, () =>
            {
                return new ConfigurationVisitor<T>().GetNodes(expression);
            });
        }

        private string GenerateCacheKey<T>(string scheme = null)
        {
            var key = typeof(T).FullName;
            if (!String.IsNullOrEmpty(scheme))
            {
                key += scheme;
            }
            return key;
        }      
    }
}
