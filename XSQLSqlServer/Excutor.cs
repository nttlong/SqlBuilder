using System;
using System.Collections.Generic;
using System.Text;
using XSQL;

namespace XSQLSqlServer
{
    internal class SqlCompier : ISqlCompiler
    {
        public string Compile(FuncExpr funcExpr, List<XSqlCommandParam> Params)
        {
            throw new NotImplementedException();
        }

        public string GetBrackets()
        {
            throw new NotImplementedException();
        }

        public string GetParamPrefix()
        {
            throw new NotImplementedException();
        }
    }
    public class Excutor : IExcutor
    {
        SqlCompier compiler = new SqlCompier();
        public XSqlCommand GetCommand(object SqlObj)
        {
            var Sql = SqlObj as BaseSql;
            if (Sql == null)
            {
                throw new Exception(string.Format("The argument is not '{0}'", typeof(BaseSql).FullName));
            }
            var sqlParams = new List<XSqlCommandParam>();
            var sql = Sql.ToSQLString("[]", "@", sqlParams, compiler);
            return new XSqlCommand
            {
                CommandText=sql,
                Params=sqlParams
            };
            
        }
    }
}
