using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XSQL
{
    public class PgCompiler : ISqlCompiler
    {
        private static ISqlCompiler ins;

        public static ISqlCompiler Provider
        {
            get
            {
                if (ins == null)
                {
                    ins = new PgCompiler();
                }
                return ins;
            }
        }

        public string Compile(FuncExpr funcExpr, List<XSqlCommandParam> Params)
        {
            if (funcExpr.Name == "Concat")
            {
                return funcExpr.Name + "(" + string.Join(",", funcExpr.Arguments.Select(p => p.ToSQLString(this.GetBrackets(), this.GetParamPrefix(), Params, this)));
            }
            if (funcExpr.Name == "Case")
            {
                var WhenClause = new List<string>();
                for (var i = 0; i < funcExpr.Arguments.Count / 2; i++)
                {
                    WhenClause.Add(
                        " when " + funcExpr.Arguments[i].ToSQLString(this.GetBrackets(), this.GetParamPrefix(), Params, this) +
                        " Then " + funcExpr.Arguments[i].ToSQLString(this.GetBrackets(), this.GetParamPrefix(), Params, this));
                }
                if (funcExpr.Arguments.Count % 2 == 0)
                {
                    return "Case " + string.Join("", WhenClause) + " Else Null";
                }
                else
                {
                    return "Case " + string.Join("", WhenClause) + 
                            " Else "+ funcExpr.Arguments[funcExpr.Arguments.Count-1].ToSQLString(this.GetBrackets(),this.GetParamPrefix(),Params,this);
                }
                
            }
            throw new NotImplementedException();
        }



        public string GetBrackets()
        {
            return @"""""";
        }

        public string GetParamPrefix()
        {
            return "@";
        }
    }
}
