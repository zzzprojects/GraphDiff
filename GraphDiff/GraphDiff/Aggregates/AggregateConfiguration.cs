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
    /// <summary>
    /// Allows creation of default mappings via a fluent interface
    /// </summary>
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

        private IAggregateRegister _register;

        private AggregateConfiguration() 
        {
            _register = new AggregateRegister(new CacheProvider());
        }

        /// <summary>
        /// Clears all mappings from the register
        /// </summary>
        public AggregateConfiguration ClearAll()
        {
            _register.ClearAll();
            return this;
        }

        /// <summary>
        /// Add a default aggregate type mapping to the register
        /// </summary>
        /// <typeparam name="T">Type of entity</typeparam>
        /// <param name="mapping">Default aggregate mapping</param>
        public AggregateConfiguration Register<T>(Expression<Func<IUpdateConfiguration<T>, object>> mapping) where T : class
        {
            return Register<T>(null, mapping);
        }

        /// <summary>
        /// Add a named aggregate type mapping to the register
        /// </summary>
        /// <typeparam name="T">Type of entity</typeparam>
        /// <param name="scheme">The name of the mapping scheme</param>
        /// <param name="mapping">Default aggregate mapping</param>
        public AggregateConfiguration Register<T>(string scheme, Expression<Func<IUpdateConfiguration<T>, object>> mapping) where T : class
        {
            var root = new ConfigurationGraphBuilder().BuildGraph<T>(mapping);
            _register.Register<T>(root, scheme);
            return this;
        }
    }
}
