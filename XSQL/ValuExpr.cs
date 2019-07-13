using System;
using System.Collections;
using System.Collections.Generic;

namespace XSQL
{
    public class ValueExpr
    {
        public Type Type { get;  set; }
        public object Val { get;  set; }
        public string AliasName { get;  set; }

        public override string ToString()
        {
            return ToSQLString("[]","@",null);
        }
        public string ToSQLString(string Quotes, string paramPrefix, List<XSqlCommandParam> Params)
        {
            if (Params == null)
            {
                var ret = string.Format("<{0}>('{1}')", this.Type.FullName, Val);
                if (this.AliasName != null)
                {
                    ret += " " + string.Format(string.Format("{0}{{0}}{1}", Quotes[0], Quotes[1]), this.AliasName);
                }
                return ret;
            }
            else
            {
                var ParamName = string.Format("{0}{1}",  paramPrefix, "p" + Params.Count);
                Params.Add(new XSqlCommandParam()
                {
                    DataType=this.Type,
                    Value=this.Val,
                    Name=ParamName
                });
                var ret = ParamName;
                return ret;
            }
        }

        internal ValueExpr Clone()
        {
            return new ValueExpr
            {
                Type=this.Type,
                Val=this.Val,
                AliasName=this.AliasName
            };
        }
    }
}