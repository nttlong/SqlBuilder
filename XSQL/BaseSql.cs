using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace XSQL
{
    public class BaseSql
    {
        public List<TreeExpr> SelectedFields { get; set; }
        public List<TreeExpr> GroupByFields { get; set; }
        public List<TreeExpr> HavingFields { get; set; }
        public bool IsSubQuery { get; set; }
        public Type ElementType { get; set; }

        public static T Field<T>(string FieldName)
        {
            throw new NotImplementedException();
        }

        public static Sql<T> Create<T>(string Schema, string Table)
        {
            var ret = new Sql<T>(Schema, Table);
            ret.MapFields = new List<MapFieldInfo>();
            ret.ParamExpr = Expression.Parameter(typeof(T), "p");
            foreach (PropertyInfo P in typeof(T).GetProperties())
            {
                var fieldName = P.Name;

                ret.MapFields.Add(new MapFieldInfo
                {
                    Member = P as PropertyInfo,
                    Name = fieldName,
                    Schema = ret.schema,
                    TableName = ret.table,

                });
            }
            return ret;
        }

        public ParameterExpression ParamExpr { get; set; }
        public List<MapFieldInfo> MapFields { get; set; }
        public string schema { get; set; }
        public ExprDataSource source { get; set; }
        public string Alias { get; set; }
        public int AliasCount { get; set; }
        public static Sql<T2> Clone<T1, T2>(Sql<T1> sql)
        {
            return Clone<T2>(sql as BaseSql);
        }
        public static void CopyTo(BaseSql sql, BaseSql ret)
        {

            ret.MapFields = sql.MapFields.ToList();
            ret.ParamExpr = sql.ParamExpr;
            ret.schema = sql.schema;
            ret.table = sql.table;
            ret.AliasCount = sql.AliasCount;
            ret.Alias = sql.Alias;
            if (sql.source != null)
            {
                ret.source = sql.source.Clone();
            }
            if (ret.SelectedFields != null)
            {
                ret.SelectedFields = sql.SelectedFields.Select(p => p.Clone()).ToList();
                ret.SelectedFields.ForEach(p =>
                {
                    if (p.Field.AliasName != null)
                    {

                    }
                });
            }
            if (sql.filter != null)
            {
                ret.filter = sql.filter.Clone();
            }
            if (sql.GroupByFields != null)
            {
                ret.GroupByFields = sql.GroupByFields.Select(p => p.Clone()).ToList();
            }
            if (sql.HavingFields != null)
            {
                ret.HavingFields = sql.HavingFields.Select(p => p.Clone()).ToList();
            }
            ret.IsSubQuery = sql.IsSubQuery || sql.filter != null || sql.source != null;

        }
        public static Sql<T2> Clone<T2>(BaseSql sql)
        {
            var ret = new Sql<T2>(true);
            CopyTo(sql, ret);
            return ret;

        }


        public string table { get; set; }
        public TreeExpr filter { get; set; }


        public static Sql<T1> Create<T1>(string Schema, string Table, Expression<Func<object, T1>> Expr)
        {
            var ret = new Sql<T1>(Schema, Table);
            if (Expr.Body is NewExpression)
            {
                ret.MapFields = new List<MapFieldInfo>();
                var nx = Expr.Body as NewExpression;
                ret.ParamExpr = Expr.Parameters[0];
                foreach (var x in nx.Members)
                {
                    var arg = nx.Arguments[nx.Members.IndexOf(x)];
                    var fieldName = x.Name;
                    if ((arg is MethodCallExpression) &&
                        (((MethodCallExpression)arg).Method.DeclaringType == typeof(BaseSql)) &&
                        (((MethodCallExpression)arg).Method.Name == "Field"))
                    {
                        fieldName = Expression.Lambda(((MethodCallExpression)arg).Arguments[0]).Compile().DynamicInvoke() as string;
                    }
                    ret.MapFields.Add(new MapFieldInfo
                    {
                        Member = x as PropertyInfo,
                        Name = fieldName,
                        Schema = ret.schema,
                        TableName = ret.table,

                    });
                }

            }
            else if (Expr.Body is MemberInitExpression)
            {
                ret.MapFields = new List<MapFieldInfo>();
                var nx = Expr.Body as MemberInitExpression;
                ret.ParamExpr = Expr.Parameters[0];
                foreach (MemberAssignment x in nx.Bindings)
                {
                    var arg = x.Expression;
                    var fieldName = x.Member.Name;
                    if ((arg is MethodCallExpression) &&
                        (((MethodCallExpression)arg).Method.DeclaringType == typeof(BaseSql)) &&
                        (((MethodCallExpression)arg).Method.Name == "Field"))
                    {
                        fieldName = Expression.Lambda(((MethodCallExpression)arg).Arguments[0]).Compile().DynamicInvoke() as string;
                    }
                    ret.MapFields.Add(new MapFieldInfo
                    {
                        Member = x.Member as PropertyInfo,
                        Name = fieldName,
                        Schema = ret.schema,
                        TableName = ret.table,

                    });
                }
            }
            return ret;
        }


        //[System.Diagnostics.DebuggerStepThrough]
        public string ToSQLString(string Quotes, string paramPrefix, List<XSqlCommandParam> Params, ISqlCompiler compiler)
        {
            var ret = "";
            if (this.source == null)
            {
                if (SelectedFields == null || this.SelectedFields.Count == 0)
                {
                    if (this.filter == null)
                    {
                        ret = "select * From " + this.GetFromClause(Quotes, paramPrefix, Params, compiler);
                    }
                    else
                    {
                        ret = "select * From " + this.GetFromClause(Quotes, paramPrefix, Params, compiler) + " where " + this.filter.ToSQLString(Quotes, paramPrefix, Params, compiler);
                    }
                }
                else
                {
                    if (this.filter == null)
                    {
                        ret = "select " + string.Join(",", SelectedFields.Select(p => p.ToSQLString(Quotes, paramPrefix, Params, compiler))) + " From " + this.GetFromClause(Quotes, paramPrefix, Params, compiler);
                    }
                    else
                    {
                        ret = "select " + string.Join(",", SelectedFields.Select(p => p.ToSQLString(Quotes, paramPrefix, Params, compiler))) + " From " + this.GetFromClause(Quotes, paramPrefix, Params, compiler) + " where " + this.filter.ToSQLString(Quotes, paramPrefix, Params, compiler);
                    }
                }
            }
            else
            {
                if (SelectedFields == null || this.SelectedFields.Count == 0)
                {
                    if (this.filter == null)
                    {
                        ret = "select * From " + this.source.ToSQLString(Quotes, paramPrefix, Params, compiler);
                    }
                    else
                    {
                        ret = "select * From " + this.source.ToSQLString(Quotes, paramPrefix, Params, compiler) + " where " + this.filter.ToSQLString(Quotes, paramPrefix, Params, compiler);
                    }
                }
                else
                {
                    if (this.filter == null)
                    {
                        ret = "select " + string.Join(",", SelectedFields.Select(p => p.ToSQLString(Quotes, paramPrefix, Params, compiler))) + " From " + this.source.ToSQLString(Quotes, paramPrefix, Params, compiler);
                    }
                    else
                    {
                        ret = "select " + string.Join(",", SelectedFields.Select(p => p.ToSQLString(Quotes, paramPrefix, Params, compiler))) + " From " + this.source.ToSQLString(Quotes, paramPrefix, Params, compiler) + " where " + this.filter.ToSQLString(Quotes, paramPrefix, Params, compiler);
                    }
                }
            }
            if (this.GroupByFields != null)
            {
                ret += " Group By " + string.Join(",", this.GroupByFields.Select(p => p.ToSQLString(Quotes, paramPrefix, Params, compiler)));
            }
            if (ret != "")
            {
                return ret;
            }
            throw new NotImplementedException();
        }

        private string GetFromClause(string quotes, string paramPrefix, List<XSqlCommandParam> Params, ISqlCompiler compiler)
        {
            if (this.schema != null)
            {
                return string.Format(string.Format("{0}{{0}}{1}.{0}{{1}}{1}", quotes[0], quotes[1]), this.schema, this.table);
            }
            else
            {
                return string.Format(string.Format("{0}{{0}}{1}", quotes[0], quotes[1]), this.table);
            }
        }
        public override string ToString()
        {
            return this.ToSQLString("[]", "@", null, null);
        }


    }
}