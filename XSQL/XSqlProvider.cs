using System.Linq;
using System.Linq.Expressions;

namespace XSQL
{
    internal class XSqlProvider: IQueryProvider
    {
        private object sql;

        public XSqlProvider(object sql)
        {
            this.sql = sql;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            throw new System.NotImplementedException();
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression Expr)
        {
            if(Expr is MethodCallExpression)
            {
                var cx = Expr as MethodCallExpression;
                if(cx.Method.Name== "SelectMany")
                {
                    return ExprCompiler.DoSelectMany<TElement>(cx);
                }
                if (cx.Method.Name == "Select")
                {
                    return ExprCompiler.DoSelect<TElement>(cx);
                }
                if (cx.Method.Name == "Join")
                {
                    var qr1 = Expression.Lambda(cx.Arguments[0]).Compile().DynamicInvoke() as BaseSql;
                    var qr2 = Expression.Lambda(cx.Arguments[1]).Compile().DynamicInvoke() as BaseSql;
                    var leftKey = (MemberExpression)((LambdaExpression)((UnaryExpression)cx.Arguments[2]).Operand).Body;
                    var rightKey = (MemberExpression)((LambdaExpression)((UnaryExpression)cx.Arguments[3]).Operand).Body;
                    var selector = cx.Arguments[4];
                    return ExprCompiler.DoInnerJoin<TElement>(qr1, qr2, leftKey, rightKey, selector);
                }
            }
            
            throw new System.NotImplementedException();
        }

        public object Execute(Expression expression)
        {
            throw new System.NotImplementedException();
        }

        public TResult Execute<TResult>(Expression expression)
        {
            throw new System.NotImplementedException();
        }
    }
}