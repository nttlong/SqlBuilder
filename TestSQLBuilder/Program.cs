using SqlPreBuilder;
using System;

namespace TestSQLBuilder
{
    class Program
    {
        static void Main(string[] args)
        {
           
            var ret1 = SqlPreBuilder.Factory.Create<clsTest>("dbo", "Test");
            var ret2 = SqlPreBuilder.Factory.Create<clsTest>("dbo", "Test");
            ret1.AliasName = "qr1";
            ret2.AliasName = "qr2";
            var ret3 = SqlPreBuilder.SQL.Combine(ret1, ret2, (p, q) => new {
                p,q
            });
            Console.WriteLine(SqlPreBuilder.SQL.GetSql(ret1, "[]"));
            ret1 = SqlPreBuilder.SQL.Select(ret1,p => new { Code = p.Code + "/" + "123" });
            Console.WriteLine(SqlPreBuilder.SQL.GetSql(ret1, "[]"));
            Console.WriteLine(SqlPreBuilder.SQL.GetSql(ret3, "[]"));
            //string sql = SqlPreBuilder.SQL.GetSql(ret3, "[]");
            //foreach(var x in ret.Fields)
            //{
            //    Console.WriteLine(string.Format("{0}.{1}.{2}", x.Schema, x.Table, x.Name));
            //}
            Console.ReadKey();
        }
    }
}
