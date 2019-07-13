
using System;
using XSQL;
using System.Linq;
using System.Collections.Generic;

namespace TestSQLBuilder
{
    class Program
    {

        
        static void Main(string[] args)
        {
            var qr = new XSQL.Sql<GnolMembership.Models.SysUsers>();
            var sql1 = from x in qr
                       where x.Username == "admin"
                       group x by x.Username into g
                       select g;
            var sql3 = from x in sql1
                       join y in qr
                       on x.First().Username equals y.Username
                       select new {
                           FX=SqlFn.Concat( x.First().Username,"123")
                       };
           Console.WriteLine(sql3);
            //var fx = ExprParse.Parse("(fx(sum(1,1)))");
            //Func<string> f1 = () =>
            // {
            //     return "Code12";

            // };

            //var sql = BaseSql.Create("x", "y", p => new MyClass {
            //    Code=BaseSql.Field<int>(f1()),
            //    X=12
            //});
            
            var sql = BaseSql.Create<MyClass>(null, "DM_Objects");
            //var sql2 = from v in sql
            //          select new Class3
            //          {
            //              Code2=v.Code+"123"
            //          };
            //var f1 = new List<string>();
            //var f2 = new List<string>();
            //f1.Join(f2, p => p, q => q, (p, q) => p);
            //var F1 = new List<clsTest>();
            //var F2 = from f in F1
            //         select new
            //         {
            //             C=f.Code
            //         };
            //var F3 = from a in F1
            //         join b in F2
            //         on a.Code equals b.C
            //         select new
            //         {

            //         };
            //sql.Join(sql2, (p,q) => p.CodeName== q.Code2, (p, q) => new Class4 { });
            sql.ToString();
            var qr1 = from x in sql select x;

            var qr2 = from v in sql select v;
                      
            //var sql1 = from x in qr1
            //           from v in qr2.Where(p=>p.ObjectId!=SqlFn.Concat(x.ObjectId,"123")).DefaultIfEmpty()
                       
            //           select new Test001
            //           {
                          
            //               Code=SqlFn.Concat("123","A"),
            //               Test=SqlFn.Case(x.ObjectId=="A","A","B")
            //           };

            //var testSQL = from x in sql
            //              from y in sql1.Where(y=>y.Code==x.ObjectId+"123").DefaultIfEmpty()
            //              select new  {
            //                  x=x

            //              };
            var cmd = XCommand.GetCommand(PgCompiler.Provider, sql1);
                       //join m in sql
                       //on x.ObjectId equals m.ObjectId
                       //select new
                       //{
                       //    x,
                       //    v,
                       //    Key=v.ObjectId+"/"
                       //};
            //var sqlF = sql.Join(sql1, (p, q)=>p.ObjectId!=q.Key, (p, q) => new {

            //});
            //Console.Write(sql1.source.ToSQLString(@""""""));
            //var sql1 = from x in sql
            //           select new FX
            //           {
            //               VV1=x.Code+"/"
            //           };
            //var sql3 = sql1.Select(p => new FX { MM = p.VV1 + "xxx" }).Where(p=>p.MM=="123");

            //var qr = sql3.Join(sql, (p, q) => p.MM==q.CodeName, (p, q) => p);
            //var cr = qr.Select(p => new
            //{
            //    p.MM
            //});
            //var fx = cr.ToString();
            //sql=sql.Select("Code", "X");
            //var sql2 = BaseSql.Create("a", "b", p => new {
            //    Code6 = BaseSql.Field<int>(f1()),
            //    Xi = 12
            //});
            //var t = from m in sql
            //        from x in sql2
            //       // on m.Code equals x.Code6
            //        select new {v=m.Code+"/"+x.Code6,  m,x };

            //var sql1 = from x in t
            //           from a in sql
            //           select new
            //           {
            //               vv=x.v+"b"
            //           };

            //var sql2 = sql1.Where(p => p.fx == 123);
            //var sql3=sql2.Select(p=>new {p.X }).Where(v=>v.X==12);
            // var x = sql3.ToSQLString("[]");
            Console.ReadKey();
        }
    }
}
