using RefactorThis.GraphDiff.Attributes;
using RefactorThis.GraphDiff.Internal.Graph;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Runtime.Caching;
using RefactorThis.GraphDiff.Internal.Caching;
using RefactorThis.GraphDiff.Internal.GraphBuilders;
using System.Linq.Expressions;

namespace RefactorThis.GraphDiff.Internal
{
    internal class AggregateRegister
    {
        private readonly ICacheProvider _cache;
        private readonly IAttributeGraphBuilder _attributeGraphBuilder;

        public AggregateRegister(ICacheProvider cache)
        {
            _cache = cache;
            _attributeGraphBuilder = new AttributeGraphBuilder();
        }

        public void Register<T>(GraphNode rootNode, string scheme = null)
        {
            _cache.Insert(GenerateCacheKey<T>(scheme), rootNode);
        }

        public GraphNode GetEntityGraph<T>(string scheme = null)
        {
            return _cache.GetOrAdd<GraphNode>(GenerateCacheKey<T>(scheme), () =>
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
            // TODO caching not implemented, add code to implement caching of configuration mappings
            //return _cache.GetOrAdd<GraphNode>(key, () =>
            //{
                return new ConfigurationVisitor<T>().GetNodes(expression);
            //});
        }

        private string GenerateCacheKey<T>(string scheme = null)
        {
            var key = typeof(AggregateRegister).FullName + typeof(T).FullName;
            if (!String.IsNullOrEmpty(scheme))
            {
                key += scheme;
            }
            return key;
        }      
    }
}
