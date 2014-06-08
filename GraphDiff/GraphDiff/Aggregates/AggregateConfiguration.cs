using RefactorThis.GraphDiff.Internal;
using RefactorThis.GraphDiff.Internal.Caching;
using RefactorThis.GraphDiff.Internal.GraphBuilders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace RefactorThis.GraphDiff.Aggregates
{
    public sealed class AggregateConfiguration
    {
        private static readonly AggregateConfiguration _aggregates = new AggregateConfiguration();
        public static AggregateConfiguration Aggregates
        {
            get
            {
                return _aggregates;
            }
        }

        private AggregateRegister _register;

        private AggregateConfiguration() 
        {
            _register = new AggregateRegister(new CacheProvider());
        }

        public AggregateConfiguration Register<T>(Expression<Func<IUpdateConfiguration<T>, object>> mapping) where T : class
        {
            return Register<T>(null, mapping);
        }

        public AggregateConfiguration Register<T>(string scheme, Expression<Func<IUpdateConfiguration<T>, object>> mapping) where T : class
        {
            var root = new ConfigurationVisitor<T>().GetNodes(mapping);
            _register.Register<T>(root, scheme);
            return this;
        }
    }
}
