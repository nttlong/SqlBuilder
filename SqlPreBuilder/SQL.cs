using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace SqlPreBuilder
{
    public class SQL
    {
        public static SQLInfo<T> Select<T,T1>(SQLInfo<T> sql, Expression<Func<T, T1>> Expr)
        {
           if(Expr.Body is NewExpression)
            {
                return SelectByNewExpression(sql,Expr.Parameters[0], (NewExpression)Expr.Body);
            }
            var Field = sql.Fields.FirstOrDefault(
                p => p.ParamExpr == Expr.Parameters[0] &&
                p.Property == ((MemberExpression)Expr.Body).Member
            );
            if (Field != null)
            {
                Field.Expr= Compiler.Complie(sql, Expr.Body);
                
            }
            return sql;
        }

        public static SQLInfo<T> Combine<T1,T2,T>(SQLInfo<T1> sql1, SQLInfo<T2> sql2, Expression<Func<T1, T2, T>> Selector)
        {
            var ret = new SQLInfo<T>();
            if (sql1.Params != null)
            {
                ret.Params.AddRange(sql1.Params);
            }
            if (sql1.Params != null)
            {
                ret.Params.AddRange(sql2.Params);
            }
            ret.SelectFields = Combinator.SelectFields(ret, sql1.Fields, sql2.Fields, Selector.Parameters[0], Selector.Parameters[1], sql1.AliasName, sql2.AliasName, Selector.Body);
            return ret;
        }

        public static string GetSql<T>(SQLInfo<T> sql,string Quote)
        {
            var fromStr =string.Format("{0}{{0}}{1}.{0}{{1}}{1}",Quote[0],Quote[1]);
            var ret = "select " + GetSqlSelectFields(sql, Quote);
            ret += " from " + string.Format(fromStr, sql.SchemaName, sql.TableName);
            return ret;
        }

        public static string GetSqlSelectFields<T>(SQLInfo<T> sql, string Quote)
        {
            var strFormat= string.Format("{0}{{0}}{1}.{0}{{1}}{1}", Quote[0], Quote[1]);
            var strFormat2 = string.Format("{0}{{0}}{1}.{0}{{1}}{1}.{0}{{2}}.{1}", Quote[0], Quote[1]);
            var r = new List<string>();

            return string.Join(",",
                sql.SelectFields.Select(p => GetTextField(sql, p, Quote)));
        }

        public static string GetTextField<T>(SQLInfo<T> sql, DataFieldInfo p, string Quote)
        {
            var strFormat = string.Format("{0}{{0}}{1}.{0}{{1}}{1}", Quote[0], Quote[1]);
            var strAliasFormat= string.Format("{0}{{0}}{1}", Quote[0], Quote[1]);
            if (p.Expr != null)
            {
                return GetTextField(sql,  p.Expr, Quote)+" "+string.Format(strAliasFormat,p.Name);
            }
           
            if (p.Schema != null)
            {
                if (p.Name != "*")
                {
                    strFormat = string.Format("{0}{{0}}{1}.{0}{{1}}{1}.{0}{{2}}{1}", Quote[0], Quote[1]);
                    return string.Format(strFormat, p.Schema, p.Table, p.Name);
                }
                else
                {
                    strFormat = string.Format("{0}{{0}}{1}.{0}{{1}}{1}.*", Quote[0], Quote[1]);
                    return string.Format(strFormat, p.Schema, p.Table);
                }
            }
            else
            {
                if (p.Name != "*")
                {
                    return string.Format(strFormat, p.Table, p.Name);
                }
                else
                {
                    return Quote[0] + p.Table + Quote[1] + ".*";
                }
            }
            
           
        }

        private static string GetTextField<T>(SQLInfo<T> sql,  TreeExpression expr, string quote)
        {
            if (expr.Op != null)
            {
                
                return "(" + GetTextField(sql, expr.Left, quote) + ")" + expr.Op + "(" + GetTextField(sql, expr.Right, quote)+")";
            }
            if (expr.Field != null)
            {
                return GetTextField(sql, expr.Field, quote);
            }
            if (expr.Value != null)
            {
                if (sql.Params == null)
                {
                    sql.Params = new List<FieldValue>();
                }
                sql.Params.Add(expr.Value);
                return "{" + (sql.Params.Count - 1) + "}";
            }
            throw new NotImplementedException();
        }

        private static SQLInfo<T> SelectByNewExpression<T>(SQLInfo<T> sql,ParameterExpression pExpr, NewExpression Expr)
        {
            if (sql.SelectFields == null)
            {
                sql.SelectFields = new List<DataFieldInfo>();
            }
            foreach(var x in Expr.Arguments)
            {
                var mb = Expr.Members[Expr.Arguments.IndexOf(x)];
                var field = sql.Fields.FirstOrDefault(p => p.ParamExpr == pExpr && p.Property == mb);
                if (field == null)
                {
                    field = sql.Fields.FirstOrDefault(p => p.ParamExpr.Type == pExpr.Type && p.Property.Name == mb.Name);
                }
                if (field != null)
                {
                    field.Expr = Compiler.Complie(sql, x);
                    sql.SelectFields.Add(field);
                }
                else
                {
                    throw new Exception(string.Format("'{0}' was not found",mb.Name));
                }
            }
            return sql;
        }
    }
}