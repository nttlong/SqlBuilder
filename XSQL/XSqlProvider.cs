using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

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
                    var ret= ExprCompiler.DoSelectByMethodCallExpression<TElement>(cx) as BaseSql;
                    if (ret.MapFields == null)
                    {
                        ret.MapFields = new List<MapFieldInfo>();
                    }
                    
                    var mbxs = ExprCompiler.GetAllMemberExpression(Expr);
                    foreach(var mbx in mbxs)
                    {
                        var mp = ret.MapFields.FirstOrDefault(p => p.ParamExpr == mbx.Expression && p.Member == mbx.Member);
                        if (mp == null)
                        {
                            ret.MapFields.Add(new MapFieldInfo
                            {
                                Alias=ret.Alias,
                                AliasName=mbx.Member.Name,
                                Member=mbx.Member as PropertyInfo,
                                ParamExpr=mbx.Expression as ParameterExpression,
                                Name=mbx.Member.Name,
                                Schema=ret.schema,
                                TableName=ret.table
                            });
                        }
                        
                    }
                    typeof(TElement).GetProperties().ToList().ForEach(p =>
                    {
                        ret.MapFields.Add(new MapFieldInfo
                        {
                            Member = p,
                            ParamExpr = Expression.Parameter(p.PropertyType, "p"),
                            Name = p.Name
                        });
                    });
                    return ret as IQueryable<TElement>;
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
                if (cx.Method.Name == "GroupBy")
                {
                    return ExprCompiler.DoGroupBy<TElement>(cx) as IQueryable<TElement>;
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