using System;
using System.Collections.Generic;
using System.Linq;

namespace XSQL
{
    public class FuncExpr
    {
        public string Name { get; internal set; }
        public List<TreeExpr> Arguments { get; set; }
        public string AliasName { get; internal set; }

        public string ToSQLString(string Quotes, string paramPrefix, List<XSqlCommandParam> Params,ISqlCompiler compiler)
        {
            if (Params == null)
            {
                return Name + "(" + string.Join(",", Arguments.Select(p => p.ToSQLString(Quotes, paramPrefix, Params, compiler)))+")";

            }
            else
            {
                return compiler.Compile(this.Clone(), Params);
            }
            throw new NotImplementedException();
        }

        public FuncExpr Clone()
        {
            return new FuncExpr
            {
                Arguments=this.Arguments.Select(p=>p.Clone()).ToList(),
                Name=Name
            };
        }

        public override string ToString()
        {
            return ToSQLString("[]", "@", null,null);
        }
    }
}