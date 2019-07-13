using System;
using System.Collections;
using System.Collections.Generic;

namespace XSQL
{
    public class TreeExpr
    {
        public string Op { get;  set; }
        public TreeExpr Left { get;  set; }
        public TreeExpr Right { get; set; }
        public FieldExpr Field { get;  set; }
        public ValueExpr Value { get;  set; }
        public string AliasName { get; internal set; }
        internal FuncExpr Callee { get; set; }

        //[System.Diagnostics.DebuggerStepThrough]
        public string ToSQLString(string Quotes, string paramPrefix, List<XSqlCommandParam> Params, ISqlCompiler compiler)
        {
            if (this.Value != null)
            {
                return this.Value.ToSQLString(Quotes, paramPrefix, Params);
            }
            else if (this.Field != null)
            {
                return this.Field.ToSQLString(Quotes, paramPrefix);
            }
            else if (Op!=null)
            {
                return "(" + Left.ToSQLString(Quotes, paramPrefix, Params, compiler) + ")" + Op + "(" + Right.ToSQLString(Quotes, paramPrefix, Params, compiler) + ") " + ((this.AliasName != null) ? string.Format(string.Format("{0}{{0}}{1}", Quotes[0], Quotes[1]), this.AliasName):"");
            }
            else if (Callee != null)
            {
                if (Callee.AliasName == null)
                {
                    return Callee.ToSQLString(Quotes, paramPrefix, Params, compiler);
                }
                else
                {
                    return Callee.ToSQLString(Quotes, paramPrefix, Params, compiler)+" "+string.Format(string.Format("{0}{{0}}{1}",Quotes[0],Quotes[1]),this.AliasName);
                }
            }
            return "Unknown";
        }

        public TreeExpr SetTableName(string tableName)
        {
            if (this.Field != null)
            {
                this.Field.Schema = null;
                this.Field.TableName = tableName;
            }
            if (this.Op != null)
            {
                this.Left.SetTableName(tableName);
                this.Right.SetTableName(tableName);
            }
            return this;
        }

        public override string ToString()
        {
            return this.ToSQLString("[]","@",null,null);
        }

        public TreeExpr Clone()
        {
            if (this.Value != null)
            {
                return new TreeExpr
                {
                    Value = this.Value.Clone()
                };
            }
            else if (this.Field != null)
            {
                return new TreeExpr
                {
                    Field=this.Field.Clone()
                };
            }
            else if (this.Op != null)
            {
                return new TreeExpr
                {
                    Op=Op,
                    Left=this.Left.Clone(),
                    Right=this.Right.Clone(),
                    AliasName=this.AliasName
                };
            }
            else if (this.Callee != null)
            {
                return new TreeExpr
                {
                    Callee=Callee.Clone()
                };
            }
            throw new NotImplementedException();
        }

        public TreeExpr ClearAliasName()
        {
            this.AliasName = null;
            if (this.Field != null)
            {
                this.Field.ClearAliasName();
            }
            if (this.Op != null)
            {
                this.Left.ClearAliasName();
                this.Right.ClearAliasName();
            }
            if (this.Value != null)
            {
                this.Value.ClearAliasName();
            }
            return this;
        }
    }
}