using System;
using System.Linq.Expressions;

namespace SqlPreBuilder
{
    internal class Utils
    {
        public static string GetStrValue(Expression Expr)
        {
            return (Expression.Lambda(Expr).Compile().DynamicInvoke()).ToString();
        }

        public static string GetOp(ExpressionType nodeType)
        {
            if (nodeType == ExpressionType.Add)
            {
                return "+";
            }
            throw new NotSupportedException();
        }
    }
}