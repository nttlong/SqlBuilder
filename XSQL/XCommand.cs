using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XSQL
{
    public class XCommand
    {
        private static XSqlCommand GetCommand(ISqlCompiler Compiler, BaseSql Sql,string Bracket,string ParamPrefix)
        {
            var Params = new List<XSqlCommandParam>();
            var ret = new XSqlCommand
            {
                
                CommandText=Sql.ToSQLString(Bracket,ParamPrefix, Params, Compiler)
            };
            ret.Params = Params;
            var selectFields = new List<string>();
            
            
            return ret;
        }

        private static XSqlCommand GetCommand<T>(ISqlCompiler Compiler, IQueryable<T> Sql, string Bracket, string ParamPrefix)
        {
            return GetCommand(Compiler,(BaseSql)Sql, Bracket, ParamPrefix);
        }

        public static XSqlCommand GetCommand<T>(ISqlCompiler provider, IQueryable<T> sql)
        {
            return GetCommand(provider, (BaseSql)sql, provider.GetBrackets(), provider.GetParamPrefix());
        }
    }
}
