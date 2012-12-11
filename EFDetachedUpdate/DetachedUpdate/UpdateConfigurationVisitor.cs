/*
 * This code is provided as is with no warranty. If you find a bug please report it on github.
 * If you would like to use the code please leave this comment at the top of the page
 * (c) Brent McKendrick 2012
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

// This should be internal only
namespace RefactorThis.EFExtensions.Internal
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
        public Expression IncludeExpression { get; set; }
        public bool IsCollection { get; set; }
        public bool IsOwned { get; set; }

        public bool HasMembers()
        {
            return Members.Count > 0;
        }
    }

    /// <summary>
    /// Used as a translator from the expression tree to the UpdateMember tree
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class UpdateConfigurationVisitor<T> : ExpressionVisitor
    {
        UpdateMember currentMember;
        UpdateMember previousMember = null;
        string currentMethod = "";

        /// <summary>
        /// Translates the Expression tree to a tree of UpdateMembers
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns></returns>
        public UpdateMember GetUpdateMembers(Expression<Func<IUpdateConfiguration<T>, object>> expression)
        {
            var initialNode = new UpdateMember() { IncludeExpression = Expression.Parameter(typeof(T), "p") };
            currentMember = initialNode;
            Visit(expression);
            return initialNode;
        }

        protected override Expression VisitMember(MemberExpression expression)
        {
            // Create new node for this item
            var newMember = new UpdateMember 
            { 
                Members = new Stack<UpdateMember>(),
                Parent = currentMember,
                Accessor = (PropertyInfo)expression.Member
            };

            currentMember.Members.Push(newMember);
            previousMember = currentMember;
            currentMember = newMember;
            currentMember.IncludeExpression = CreateIncludeExpression(previousMember, currentMember);

            // Chose if entity update or reference update and create expression
            switch (currentMethod)
            {
                case "OwnedEntity":
                    currentMember.IsOwned = true;
                    break;
                case "AssociatedEntity":
                    currentMember.IsOwned = false;
                    break;
                case "OwnedCollection":
                    currentMember.IsOwned = true;
                    currentMember.IsCollection = true;
                    break;
                case "AssociatedCollection":
                    currentMember.IsOwned = false;
                    currentMember.IsCollection = true;
                    break;
                default:
                    throw new NotSupportedException("The method used in the update mapping is not supported");
            }
            return base.VisitMember(expression);
        }

        private Expression CreateIncludeExpression(UpdateMember previousMember, UpdateMember currentMember)
        {
            // Add First() for Include expression if previous was a collection
            if (previousMember.IsCollection)
            {
                var previousType = ((MemberExpression)previousMember.IncludeExpression).Type;
                var currentType = currentMember.Accessor.PropertyType;

                if (previousType.IsGenericType && previousType.GetGenericArguments().Length == 1)
                {
                    var innerParam = Expression.Parameter(previousType.GetGenericArguments()[0]);
                    return Expression.Call(
                        typeof(Enumerable),
                        "Select",
                        new[] { previousType.GetGenericArguments()[0], currentType },
                        previousMember.IncludeExpression,
                        Expression.Lambda(
                            Expression.Property(innerParam, currentMember.Accessor)
                        , innerParam));
                }
                else
                    throw new NotSupportedException("Only supports generic typed collections of IEnumerable<T>");
            } 
            else
                return Expression.Property(previousMember.IncludeExpression, (PropertyInfo)currentMember.Accessor);
        }


        protected override Expression VisitMethodCall(MethodCallExpression expression)
        {
            currentMethod = expression.Method.Name;

            // go left to right in the subtree (ignore first argument for now)
            for (int i = 1; i < expression.Arguments.Count; i++)
                Visit(expression.Arguments[i]);

            // go back up the tree and continue
            currentMember = currentMember.Parent;
            return Visit(expression.Arguments[0]);
        }
    }

    internal static class EFIncludeHelper
    {
        public static List<Expression<Func<T, object>>> GetIncludeExpressions<T>(UpdateMember member)
        {
            var expressions = new List<Expression<Func<T, object>>>();
            GetIncludeExpressions<T>(member, Expression.Parameter(typeof(T), "p"), expressions);
            return expressions;
        }

        private static void GetIncludeExpressions<T>(UpdateMember member, ParameterExpression parameter, List<Expression<Func<T, object>>> expressions)
        {
            if (!member.HasMembers())
                expressions.Add(Expression.Lambda<Func<T, object>>(member.IncludeExpression, parameter));
            else
            {
                foreach (var iMember in member.Members)
                {
                    GetIncludeExpressions<T>(iMember, parameter, expressions);
                }
            }
        }
    }
}
