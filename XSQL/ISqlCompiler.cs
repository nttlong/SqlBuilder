using System;
using System.Collections.Generic;
using System.Text;

namespace XSQL
{
    public interface ISqlCompiler
    {
        
        string GetBrackets();
        string GetParamPrefix();
        string Compile(FuncExpr funcExpr, List<XSqlCommandParam> Params);
    }
}
