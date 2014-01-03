/*
 * This code is provided as is with no warranty. If you find a bug please report it on github.
 * If you would like to use the code please leave this comment at the top of the page
 * License MIT (c) Brent McKendrick 2012
 */

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

// This should be internal only
namespace RefactorThis.GraphDiff.Internal
{
    /// <summary>
    /// Used internally to represent the update graph
    /// </summary>
    internal class UpdateMember
    {
        public UpdateMember()
        {
            Members = new Stack<UpdateMember>();
        }
        public UpdateMember Parent { get; set; }
        public PropertyInfo Accessor { get; set; }
        public Stack<UpdateMember> Members { get; set; }
        public string IncludeString { get; set; }
        public bool IsCollection { get; set; }
        public bool IsOwned { get; set; }
    }

    /// <summary>
    /// Used as a translator from the expression tree to the UpdateMember tree
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class UpdateConfigurationVisitor<T> : ExpressionVisitor
    {
        private UpdateMember _currentMember;
        private UpdateMember _previousMember;
        private string _currentMethod = "";

        /// <summary>
        /// Translates the Expression tree to a tree of UpdateMembers
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns></returns>
        public UpdateMember GetUpdateMembers(Expression<Func<IUpdateConfiguration<T>, object>> expression)
        {
            var initialNode = new UpdateMember();
            _currentMember = initialNode;
            Visit(expression);
            return initialNode;
        }

        protected override Expression VisitMember(MemberExpression memberExpression)
        {
            // Create new node for this item
            var newMember = new UpdateMember
            {
                Members = new Stack<UpdateMember>(),
                Parent = _currentMember
            };

            // Added as a bug fix to support Expressions as variables.
            var expression = memberExpression.Expression;
            if (expression is ConstantExpression)
            {
                object container = ((ConstantExpression)expression).Value;
                var member = memberExpression.Member;
                if (member is FieldInfo)
                {
                    dynamic value = ((FieldInfo)member).GetValue(container);
                    newMember.Accessor = (PropertyInfo)value.Body.Member;
                }
                if (member is PropertyInfo)
                {
                    dynamic value = ((PropertyInfo)member).GetValue(container, null);
                    newMember.Accessor = (PropertyInfo)value.Body.Member;
                }
            }
            else
            {
                newMember.Accessor = (PropertyInfo)memberExpression.Member;
            }

            _currentMember.Members.Push(newMember);
            _previousMember = _currentMember;
            _currentMember = newMember;

            _currentMember.IncludeString = _previousMember.IncludeString != null
                ? _previousMember.IncludeString + "." + _currentMember.Accessor.Name 
                : _currentMember.Accessor.Name;

            // Chose if entity update or reference update and create expression
            switch (_currentMethod)
            {
                case "OwnedEntity":
                    _currentMember.IsOwned = true;
                    break;
                case "AssociatedEntity":
                    _currentMember.IsOwned = false;
                    break;
                case "OwnedCollection":
                    _currentMember.IsOwned = true;
                    _currentMember.IsCollection = true;
                    break;
                case "AssociatedCollection":
                    _currentMember.IsOwned = false;
                    _currentMember.IsCollection = true;
                    break;
                default:
                    throw new NotSupportedException("The method used in the update mapping is not supported");
            }
            return base.VisitMember(memberExpression);
        }

        protected override Expression VisitMethodCall(MethodCallExpression expression)
        {
            _currentMethod = expression.Method.Name;

            // go left to right in the subtree (ignore first argument for now)
            for (int i = 1; i < expression.Arguments.Count; i++)
                Visit(expression.Arguments[i]);

            // go back up the tree and continue
            _currentMember = _currentMember.Parent;
            return Visit(expression.Arguments[0]);
        }
    }

    internal static class EntityFrameworkIncludeHelper
    {
        public static List<string> GetIncludeStrings(UpdateMember root)
        {
            var list = new List<string>();

            if (root.Members.Count == 0 && root.IncludeString != null)
            {
                list.Add(root.IncludeString);
            }

            foreach (var member in root.Members)
            {
                list.AddRange(GetIncludeStrings(member));
            }
            return list;
        }
    }
}
