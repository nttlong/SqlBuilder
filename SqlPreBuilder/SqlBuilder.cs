using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace SqlPreBuilder
{
    internal class SqlBuilder
    {
        public static SQLInfo<T> Create<T>(SQLInfo<T> Sql, ParameterExpression PExpr, Expression Expr)
        {
            Sql.Fields = GetAllFields(Sql, PExpr, Expr);
            return Sql;
        }

        public static List<DataFieldInfo> GetAllFields<T>(SQLInfo<T> sql, ParameterExpression PExpr, Expression Expr)
        {
            if (Expr is NewExpression)
            {
                return GetAllFieldsInNewExpression(sql, PExpr, (NewExpression)Expr);
            }
            else if (Expr is MemberInitExpression)
            {
                return GetAllFieldsInMemberInitExpression(sql, PExpr, (MemberInitExpression)Expr);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public static List<DataFieldInfo> GetAllFieldsInMemberInitExpression<T>(SQLInfo<T> sql, ParameterExpression pExpr, MemberInitExpression expr)
        {
            var ret = new List<DataFieldInfo>();
            foreach (MemberAssignment x in expr.Bindings)
            {
                var fieldName = x.Member.Name;
                if (x.Expression is MethodCallExpression)
                {
                    var cx = x.Expression as MethodCallExpression;
                    if (cx.Method.DeclaringType == typeof(Factory))
                    {
                        if (cx.Method.Name == "Field")
                        {
                            fieldName = Utils.GetStrValue(cx.Arguments[0]);
                        }
                    }
                    ret.Add(new DataFieldInfo()
                    {
                        ParamExpr = pExpr,
                        Schema = sql.SchemaName,
                        Table = sql.TableName,
                        Name = fieldName,
                        Property = x.Member
                    });
                }

            }
            return ret;
        }

        public static List<DataFieldInfo> GetAllFieldsInNewExpression<T>(SQLInfo<T> sql, ParameterExpression PExpr, NewExpression expr)
        {
            var ret = new List<DataFieldInfo>();
            foreach (var x in expr.Members)
            {
                var fieldName = x.Name;
                if (expr.Arguments[expr.Members.IndexOf(x)] is MethodCallExpression)
                {
                    var cx = expr.Arguments[expr.Members.IndexOf(x)] as MethodCallExpression;
                    if (cx.Method.DeclaringType == typeof(Factory))
                    {
                        if (cx.Method.Name == "Field")
                        {
                            fieldName = Utils.GetStrValue(cx.Arguments[0]);
                        }
                    }

                }
                ret.Add(new DataFieldInfo()
                {
                    ParamExpr = PExpr,
                    Schema = sql.SchemaName,
                    Table = sql.TableName,
                    Name = fieldName,
                    Property = x
                });
            }
            return ret;
        }
    }
}