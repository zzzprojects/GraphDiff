using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RefactorThis.GraphDiff.Internal.Graph
{
    internal class GraphNode
    {       
        protected readonly PropertyInfo Accessor;

        protected string IncludeString
        {
            get
            {
                var ownIncludeString = Accessor != null ? Accessor.Name : null;
                return Parent != null && Parent.IncludeString != null
                        ? Parent.IncludeString + "." + ownIncludeString
                        : ownIncludeString;
            }
        }

        public GraphNode Parent { get; set; }
        public Stack<GraphNode> Members { get; private set; }

        public GraphNode()
        {
            Members = new Stack<GraphNode>();
        }

        protected GraphNode(GraphNode parent, PropertyInfo accessor)
        {
            Accessor = accessor;
            Members = new Stack<GraphNode>();
            Parent = parent;
        }

        // overridden by different implementations
        public virtual void Update<T>(IChangeTracker changeTracker, IEntityManager entityManager, T persisted, T updating) where T : class
        {
            changeTracker.UpdateItem(updating, persisted, true);

            // Foreach branch perform recursive update
            foreach (var member in Members)
            {
                member.Update(changeTracker, entityManager, persisted, updating);
            }
        }

        public List<string> GetIncludeStrings(IEntityManager entityManager)
        {
            var includeStrings = new List<string>();
            var ownIncludeString = IncludeString;
            if (!string.IsNullOrEmpty(ownIncludeString))
            {
                includeStrings.Add(ownIncludeString);
            }

            includeStrings.AddRange(GetRequiredNavigationPropertyIncludes(entityManager));

            foreach (var member in Members)
            {
                includeStrings.AddRange(member.GetIncludeStrings(entityManager));
            }

            return includeStrings;
        }

        public string GetUniqueKey()
        {
            string key = "";
            if (Parent != null && Parent.Accessor != null)
            {
                key += Parent.Accessor.DeclaringType.FullName + "_" + Parent.Accessor.Name;
            }
            else
            {
                key += "NoParent";
            }
            return key + "_" + Accessor.DeclaringType.FullName + "_" + Accessor.Name;
        }

        protected T GetValue<T>(object instance)
        {
            return (T)Accessor.GetValue(instance, null);
        }

        protected void SetValue(object instance, object value)
        {
            Accessor.SetValue(instance, value, null);
        }

        protected virtual IEnumerable<string> GetRequiredNavigationPropertyIncludes(IEntityManager entityManager)
        {
            return new string[0];
        }

        protected static IEnumerable<string> GetRequiredNavigationPropertyIncludes(IEntityManager entityManager, Type entityType, string ownIncludeString)
        {
            return entityManager
                .GetRequiredNavigationPropertiesForType(entityType)
                .Select(navigationProperty => ownIncludeString + "." + navigationProperty.Name);
        }
    }
}
