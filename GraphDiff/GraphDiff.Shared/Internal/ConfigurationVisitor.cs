using System;
using System.Linq.Expressions;
using System.Reflection;
using RefactorThis.GraphDiff.Internal.Graph;

namespace RefactorThis.GraphDiff.Internal
{
    /// <summary>
    /// Reads an IUpdateConfiguration mapping and produces an GraphNode graph.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class ConfigurationVisitor<T> : ExpressionVisitor
    {
        private GraphNode _currentMember;
        private string _currentMethod = "";
        private bool _isAssociatedCollectionWithNav;

        public GraphNode GetNodes(Expression<Func<IUpdateConfiguration<T>, object>> expression)
        {
            var initialNode = new GraphNode();
            _currentMember = initialNode;
            Visit(expression);
            return initialNode;
        }

        protected override Expression VisitMember(MemberExpression memberExpression)
        {
            var accessor = GetMemberAccessor(memberExpression);

            if (_isAssociatedCollectionWithNav)
            {
                _currentMember.AccessorCyclicNavigationProperty = accessor;
            }
            else
            {
                var newMember = CreateNewMember(accessor);

                _currentMember.Members.Push(newMember);
                _currentMember = newMember;
            }

            return base.VisitMember(memberExpression);
        }

        protected override Expression VisitMethodCall(MethodCallExpression expression)
        {
            _currentMethod = expression.Method.Name;

            if (_currentMethod.Equals("AssociatedCollection") && expression.Arguments.Count == 3)
			{  
                Visit(expression.Arguments[1]);

                try
                {
                    _isAssociatedCollectionWithNav = true;
                    Visit(expression.Arguments[2]);
                }
                catch
                {
                    throw;
                }
                finally
                {
                    _isAssociatedCollectionWithNav = false;
                }
            }
            else
            {
                // go left to right in the subtree (ignore first argument for now)
                for (int i = 1; i < expression.Arguments.Count; i++)
                {
                    Visit(expression.Arguments[i]);
                }
            }

            // go back up the tree and continue
            _currentMember = _currentMember.Parent;
            return Visit(expression.Arguments[0]);
        }

        private GraphNode CreateNewMember(PropertyInfo accessor)
        {
            GraphNode newMember;
            switch (_currentMethod)
            {
                case "OwnedEntity":
                    newMember = new OwnedEntityGraphNode(_currentMember, accessor);
                    break;
                case "AssociatedEntity":
                    newMember = new AssociatedEntityGraphNode(_currentMember, accessor);
                    break;
                case "OwnedCollection":
                    newMember = new CollectionGraphNode(_currentMember, accessor, true);
                    break;
                case "AssociatedCollection":
                    newMember = new CollectionGraphNode(_currentMember, accessor, false);
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
            var constantExpression = expression as ConstantExpression;

            if (constantExpression != null)
            {
                var container = constantExpression.Value;
                var member = memberExpression.Member;

                var fieldInfo = member as FieldInfo;
                if (fieldInfo != null)
                {
                    dynamic value = fieldInfo.GetValue(container);
                    accessor = (PropertyInfo) value.Body.Member;
                }

                var info = member as PropertyInfo;
                if (info != null)
                {
                    dynamic value = info.GetValue(container, null);
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