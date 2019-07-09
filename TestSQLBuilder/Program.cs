
using System;
using XSQL;
using System.Linq;
namespace TestSQLBuilder
{
    class Program
    {
        static void Main(string[] args)
        {
            Func<string> f1 = () =>
             {
                 return "Code";
 
             };
            var sql = BaseSql.Create("x", "y", p => new {
                Code=BaseSql.Field<int>(f1()),
                X=12
            });
            var sql2 = BaseSql.Create("a", "b", p => new {
                Code6 = BaseSql.Field<int>(f1()),
                Xi = 12
            });
            var t = from m in sql
                    from x in sql2
                   // on m.Code equals x.Code6
                    select new {v=m.Code+"/"+x.Code6,  m,x };

            var sql1 = from x in t
                       from a in sql
                       select new
                       {
                           vv=x.v+"b"
                       };

            //var sql2 = sql1.Where(p => p.fx == 123);
            //var sql3=sql2.Select(p=>new {p.X }).Where(v=>v.X==12);
            // var x = sql3.ToSQLString("[]");
            Console.ReadKey();
        }
    }
}
