using System;
using System.Collections.Generic;
using System.Linq.Expressions;


namespace SqlPreBuilder
{
    public class Factory
    {
        

        public static SQLInfo<T> Create<T>(string Schema, string TableName, Expression<Func<object, T>> Expr)
        {
            var ret= new SQLInfo<T>()
            {
                SchemaName=Schema,
                TableName=TableName
            };

            return SqlBuilder.Create(ret,Expr.Parameters[0], Expr.Body);
        }

        public static SQLInfo<T> Create<T>(string Schema, string TableName)
        {
            var ret = new SQLInfo<T>()
            {
                SchemaName = Schema,
                TableName = TableName,
                Fields=new List<DataFieldInfo>()
            };
            var ParameterExpr = Expression.Parameter(typeof(T), "p");
            ret.SelectFields = new List<DataFieldInfo>();
            ret.SelectFields.Add(new DataFieldInfo()
            {
                Name="*",
                ParamExpr=ParameterExpr,
                Schema = Schema,
                Table = TableName,
            });
            foreach(var P in typeof(T).GetProperties())
            {
                ret.Fields.Add(new DataFieldInfo()
                {
                    Name=P.Name,
                    ParamExpr= ParameterExpr,
                    Schema=Schema,
                    Table=TableName,
                    Property=P

                });
            }
            return ret;
        }

        public static T Field<T>(string FieldName)
        {
            throw new NotImplementedException();
        }
    }
}