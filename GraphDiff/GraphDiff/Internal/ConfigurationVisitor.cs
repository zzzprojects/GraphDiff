using System;
using System.Linq.Expressions;
using System.Reflection;
using RefactorThis.GraphDiff.Internal.Nodes;

namespace RefactorThis.GraphDiff.Internal
{
    internal class ConfigurationVisitor<T> : ExpressionVisitor
    {
        private AMember _currentMember;
        private string _currentMethod = "";

        public AMember GetMembers(Expression<Func<IUpdateConfiguration<T>, object>> expression)
        {
            var initialNode = new RootEntity(null, null);
            _currentMember = initialNode;
            Visit(expression);
            return initialNode;
        }

        protected override Expression VisitMember(MemberExpression memberExpression)
        {
            var accessor = GetMemberAccessor(memberExpression);
            var newMember = CreateNewMember(accessor);

            _currentMember.Members.Push(newMember);
            _currentMember = newMember;

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

        private AMember CreateNewMember(PropertyInfo accessor)
        {
            AMember newMember;
            switch (_currentMethod)
            {
                case "OwnedEntity":
                    newMember = new OwnedEntity(_currentMember, accessor);
                    break;
                case "AssociatedEntity":
                    newMember = new AssociatedEntity(_currentMember, accessor);
                    break;
                case "OwnedCollection":
                    newMember = new OwnedCollection(_currentMember, accessor);
                    break;
                case "AssociatedCollection":
                    newMember = new AssociatedCollection(_currentMember, accessor);
                    break;
                default:
                    throw new NotSupportedException("The method used in the update mapping is not supported");
            }
            return newMember;
        }

        private static PropertyInfo GetMemberAccessor(MemberExpression memberExpression)
        {
            PropertyInfo accessor = null;
            var expression = memberExpression.Expression;
            if (expression is ConstantExpression)
            {
                object container = ((ConstantExpression) expression).Value;
                var member = memberExpression.Member;
                if (member is FieldInfo)
                {
                    dynamic value = ((FieldInfo) member).GetValue(container);
                    accessor = (PropertyInfo) value.Body.Member;
                }
                if (member is PropertyInfo)
                {
                    dynamic value = ((PropertyInfo) member).GetValue(container, null);
                    accessor = (PropertyInfo) value.Body.Member;
                }
            }
            else
            {
                accessor = (PropertyInfo) memberExpression.Member;
            }

            if (accessor == null)
                throw new NotSupportedException("Unknown accessor type found!");

            return accessor;
        }
    }
}