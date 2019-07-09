using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace SqlPreBuilder
{
    internal class Combinator
    {
        [System.Diagnostics.DebuggerStepThrough]
        internal static List<DataFieldInfo> SelectFields<T>(
            SQLInfo<T> sql, 
            List<DataFieldInfo> fields1, 
            List<DataFieldInfo> fields2, 
            ParameterExpression pExpr1, 
            ParameterExpression pExpr2, 
            string Alias1,
            string Alias2,
            Expression expr)
        {
            if(expr is NewExpression)
            {
                return SelectFieldsInNewExpression(sql, fields1, fields2, pExpr1, pExpr2, Alias1, Alias2, (NewExpression)expr);
            }
            throw new NotImplementedException();
        }
        [System.Diagnostics.DebuggerStepThrough]
        private static List<DataFieldInfo> SelectFieldsInNewExpression<T>(
            SQLInfo<T> sql, 
            List<DataFieldInfo> fields1, 
            List<DataFieldInfo> fields2, 
            ParameterExpression pExpr1, 
            ParameterExpression pExpr2,
            string Alias1,
            string Alias2,
            NewExpression expr)
        {
            var ret = new List<DataFieldInfo>();
            foreach(var x in expr.Arguments)
            {
                if(x==pExpr1)
                {
                    ret.Add(new DataFieldInfo()
                    {
                        Name="*",
                        ParamExpr=pExpr1,
                        Table=Alias1
                    });
                }
                else if (x == pExpr2)
                {
                    ret.Add(new DataFieldInfo()
                    {
                        Name = "*",
                        ParamExpr = pExpr2,
                        Table = Alias2
                    });
                }
                else if(x is MemberExpression)
                {
                    var mb = x as MemberExpression;
                    
                    if (mb.Expression == pExpr1)
                    {
                        var fMb = fields2.FirstOrDefault(p => p.Property == mb.Member);
                        //if (fMb == null)
                        //{
                        //    fMb = fields2.FirstOrDefault(p => p.Property.Name == mb.Member.Name);
                            
                        //}
                        //if (fMb == null)
                        //{
                        //    fMb = fields2.FirstOrDefault(p => p.Name == mb.Member.Name);

                        //}
                        if (fMb == null)
                        {
                            throw new Exception(string.Format("{0} was not found", mb.Member));
                        }
                        ret.Add(new DataFieldInfo()
                        {
                            Name = mb.Member.Name,
                            ParamExpr = pExpr1,
                            Table = Alias1
                        });
                    }
                    else if(mb.Expression == pExpr2)
                    {
                        var fMb = fields2.FirstOrDefault(p => p.Property == mb.Member);
                        if (fMb == null)
                        {
                            throw new Exception(string.Format("{0} was not found", mb.Member));
                        }
                        ret.Add(new DataFieldInfo()
                        {
                            Name = fMb.Name,
                            ParamExpr = pExpr1,
                            Table = Alias2
                        });
                    }
                }
                else if(x is BinaryExpression)
                {
                    DataFieldInfo field = GetExpressionInBinaryExpression( 
                        expr.Members[expr.Arguments.IndexOf(x)],
                        sql, 
                        fields1, 
                        fields2, 
                        pExpr1, 
                        pExpr2, 
                        Alias1, 
                        Alias2, 
                        (BinaryExpression)x);
                    ret.Add(field);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            return ret;
        }
        //[System.Diagnostics.DebuggerStepThrough]
        private static DataFieldInfo GetExpressionInBinaryExpression<T>(
            MemberInfo memberInfo,
            SQLInfo<T> sql, 
            List<DataFieldInfo> fields1, 
            List<DataFieldInfo> fields2, 
            ParameterExpression pExpr1, 
            ParameterExpression pExpr2, 
            string alias1, 
            string alias2, 
            BinaryExpression Expr)
        {
            var ret = new DataFieldInfo();
            ret.Expr = new TreeExpression();
            ret.Expr.Op= Utils.GetOp(Expr.NodeType);
            ret.Expr.Left = GetExpression(sql, fields1, fields2, pExpr1, pExpr2, alias1, alias2, Expr.Left);
            ret.Expr.Right = GetExpression(sql, fields1, fields2, pExpr1, pExpr2, alias1, alias2, Expr.Right);
            ret.Property = memberInfo;
            ret.ParamExpr = Expression.Parameter(typeof(T),"p");
            ret.Name = memberInfo.Name;
            
            return ret;
        }
        //[System.Diagnostics.DebuggerStepThrough]
        private static TreeExpression GetExpression<T>(
            SQLInfo<T> sql, 
            List<DataFieldInfo> fields1, 
            List<DataFieldInfo> fields2,
            ParameterExpression pExpr1,
            ParameterExpression pExpr2,
            string alias1, 
            string alias2, 
            Expression Expr)
        {
            var ret = new TreeExpression();
            if(Expr is UnaryExpression)
            {
                return GetExpression(sql, fields1, fields2, pExpr1, pExpr2, alias1, alias2, ((UnaryExpression)Expr).Operand);
            }
            if(Expr is MemberExpression)
            {
                return GetExpressionInMemberExpression(sql, fields1, fields2, pExpr1, pExpr2, alias1, alias2, (MemberExpression)Expr);
            }
            return ret;
        }
        //[System.Diagnostics.DebuggerStepThrough]
        private static TreeExpression GetExpressionInMemberExpression<T>(
            SQLInfo<T> sql, 
            List<DataFieldInfo> fields1, 
            List<DataFieldInfo> fields2,
            ParameterExpression pExpr1,
            ParameterExpression pExpr2,
            string alias1, 
            string alias2, 
            MemberExpression expr)
        {
            if(expr.Expression == pExpr1)
            {
                var mb = fields1.FirstOrDefault(p => p.Property == expr.Member);
                return new TreeExpression
                {
                    Field=new DataFieldInfo
                    {
                        Name=mb.Name,
                        ParamExpr=pExpr1,
                        Property=mb.Property,
                        Table=alias1
                    }
                };
            }
            else if(expr.Expression == pExpr2)
            {
                var mb = fields2.FirstOrDefault(p => p.Property == expr.Member);
                return new TreeExpression
                {
                    Field = new DataFieldInfo
                    {
                        Name = mb.Name,
                        ParamExpr = pExpr2,
                        Property = mb.Property,
                        Table = alias2
                    }
                };
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}