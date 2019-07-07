using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace SqlPreBuilder
{
    internal class Combinator
    {
        internal static List<DataFieldInfo> SelectFields<T>(
            SQLInfo<T> sql, 
            List<DataFieldInfo> fields1, 
            List<DataFieldInfo> fields2, 
            ParameterExpression pExpr1, 
            ParameterExpression pExpr2, 
            string Alias1,
            string Alias2,
            Expression expr)
        {
            if(expr is NewExpression)
            {
                return SelectFieldsInNewExpression(sql, fields1, fields2, pExpr1, pExpr2, Alias1, Alias2, (NewExpression)expr);
            }
            throw new NotImplementedException();
        }

        private static List<DataFieldInfo> SelectFieldsInNewExpression<T>(
            SQLInfo<T> sql, 
            List<DataFieldInfo> fields1, 
            List<DataFieldInfo> fields2, 
            ParameterExpression pExpr1, 
            ParameterExpression pExpr2,
            string Alias1,
            string Alias2,
            NewExpression expr)
        {
            var ret = new List<DataFieldInfo>();
            foreach(var x in expr.Arguments)
            {
                if(x==pExpr1)
                {
                    ret.Add(new DataFieldInfo()
                    {
                        Name="*",
                        ParamExpr=pExpr1,
                        Table=Alias1
                    });
                }
            }
            return ret;
        }

       
    }
}