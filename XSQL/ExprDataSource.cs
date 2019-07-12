using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace XSQL
{
    public class ExprDataSource
    {
        internal TreeExpr filter;
        

        public List<TreeExpr> Fields { get;  set; }
        public string Alias { get;  set; }
        public string Table { get;  set; }
        public string Schema { get;  set; }
        public ExprDataSource Source { get; set; }
        public ExprDataSource LeftSource { get;  set; }
        public ExprDataSource RightSource { get;  set; }
        public TreeExpr JoinExpr { get;  set; }
        public ParameterExpression ParamExpr { get;  set; }
        public string JoinType { get;  set; }

        public string ToSQLString(string Quotes, string paramPrefix, List<XSqlCommandParam> Params, ISqlCompiler compiler)
        {
            var ret = "";
            if(this.LeftSource!=null && this.RightSource != null)
            {
                if(this.JoinExpr == null)
                {
                    return string.Join(",", new string[] { this.LeftSource.ToSQLString(Quotes, paramPrefix, Params, compiler), this.RightSource.ToSQLString(Quotes, paramPrefix, Params, compiler) });
                }
                else
                {
                    return this.LeftSource.ToSQLString(Quotes, paramPrefix, Params, compiler) + " " + this.JoinType + " join " + this.RightSource.ToSQLString(Quotes, paramPrefix, Params, compiler) + " on " + this.JoinExpr.ToSQLString(Quotes, paramPrefix, Params, compiler);
                }
            }
            if (Source != null)
            {
                if (Fields != null)
                {
                    ret = "(" + string.Join(",", Fields.Select(p => p.ToSQLString(Quotes, paramPrefix, Params, compiler))) + " from " + Source.ToSQLString(Quotes, paramPrefix, Params, compiler) +
                        ") ";
                }
                else
                {
                    ret = Source.ToSQLString(Quotes, paramPrefix, Params, compiler);
                }
                return ret;
            }
            else if(Schema !=null)
            {
                if (Fields != null)
                {
                    ret = "(" + string.Join(",", Fields.Select(p => p.ToSQLString(Quotes, paramPrefix, Params, compiler))) + " from " +
                        string.Format(string.Format("{0}{{0}}{1}.{0}{{1}}{1}", Quotes[0], Quotes[1]), Schema, Table) +
                        ") ";
                }
                else
                {
                    ret = string.Format(string.Format("{0}{{0}}{1}.{0}{{1}}{1}", Quotes[0], Quotes[1]), Schema, Table);
                    ret =  ret + " " + string.Format(string.Format("{0}{{0}}{1}", Quotes[0], Quotes[1]), Alias);
                    return ret;
                }
            }
            else if (Table!=null)
            {
                if (Fields != null)
                {
                    ret = string.Join(",", Fields.Select(p => p.ToSQLString(Quotes, paramPrefix, Params, compiler))) + " from " +
                        string.Format(string.Format("{0}{{0}}{1}", Quotes[0], Quotes[1]), Table);
                }
                else
                {
                    ret = string.Format(string.Format("{0}{{0}}{1}", Quotes[0], Quotes[1]), Table);
                }
                    
            }
            if (this.filter != null)
            {
                ret += " where " + this.filter.ToSQLString(Quotes, paramPrefix, Params, compiler);
            }
            ret = "("+ret+") "+ string.Format(string.Format("{0}{{0}}{1}", Quotes[0], Quotes[1]), Alias);
            return ret;
        }

        internal ExprDataSource Clone()
        {
            var ret = new ExprDataSource
            {
                Alias = this.Alias,
                Fields =(this.Fields!=null)? this.Fields.Select(p => p.Clone()).ToList():null,
                Schema = this.Schema,
                Source = (this.Source != null) ? this.Source.Clone() : null,
                Table = this.Table,
                filter = (this.filter != null) ? this.filter.Clone() : null,
                LeftSource = (this.LeftSource != null) ? this.LeftSource.Clone() : null,
                RightSource = (this.RightSource != null) ? this.RightSource.Clone() : null,
                ParamExpr=ParamExpr,
                JoinExpr=(JoinExpr!=null)?JoinExpr.Clone():null
            };
            return ret;
        }

        public override string ToString()
        {
            return ToSQLString("[]","@",null,null);
        }
    }
}