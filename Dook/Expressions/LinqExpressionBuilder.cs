using System;
using System.Linq.Expressions;
namespace Dook
{
    /// <summary>
    /// Static class dedicated to combine predicates.
    /// </summary>
    public class LinqExpressionBuilder<T>
    {
        Expression<Func<T, bool>> expression = null;
        public Expression<Func<T,bool>> Expression { get { return expression; } }
        /// <summary>
        /// Combines two LINQ Expressions using an OR logical operator.
        /// </summary>
        /// <returns>A LINQ Expression.</returns>
        /// <param name="expressions">A set of expressions to be combined.</param>
        public void AppendOr(Expression<Func<T, bool>> exp)
        {
            if (expression != null)
            {
                if (exp != null)
                {
                    var secondBody = expression.Body.ReplaceParameters(expression.Parameters[0], exp.Parameters[0]);
                    expression = System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(System.Linq.Expressions.Expression.OrElse(exp.Body, secondBody), exp.Parameters);
                }
            }
            else
            {
                expression = exp;
            }
        }
        /// <summary>
        /// Combines two LINQ Expressions using an AND logical operator.
        /// </summary>
        /// <returns>A LINQ Expression.</returns>
        /// <param name="expressions">Expressions.</param>
        /// <typeparam name="T">The parameter type for which the expression is built.</typeparam>
        public void AppendAnd(Expression<Func<T, bool>> exp)
        {
            if (expression != null)
            {
                if (exp != null)
                {
                    var secondBody = expression.Body.ReplaceParameters(expression.Parameters[0], exp.Parameters[0]);
                    expression = System.Linq.Expressions.Expression.Lambda<Func<T, bool>> (System.Linq.Expressions.Expression.AndAlso(exp.Body, secondBody), exp.Parameters);
                }
            }
            else
            {
                expression = exp;
            }
        }


    }

    internal class ReplaceVisitor : ExpressionVisitor
    {
        private readonly Expression from, to;
        public ReplaceVisitor(Expression from, Expression to)
        {
            this.from = from;
            this.to = to;
        }
        public override Expression Visit(Expression node)
        {
            return node == from ? to : base.Visit(node);
        }
    }

    internal static class ExpressionExtensions
    {
        public static Expression ReplaceParameters(this Expression expression, Expression searchEx, Expression replaceEx)
        {
            return new ReplaceVisitor(searchEx, replaceEx).Visit(expression);
        }
    }
}