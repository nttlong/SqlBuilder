using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace SqlPreBuilder
{
    public class SQLInfo<T>
    {
        public string TableName { get; set; }
        public string SchemaName { get; set; }
        public List<DataFieldInfo> Fields { get;  set; }
        public List<FieldValue> Params { get;  set; }
        public string AliasName { get;  set; }
        public List<DataFieldInfo> SelectFields { get;  set; }
        public int AliasCount { get; internal set; }
        public Type ElementType { get; private set; }
        public ParameterExpression ParamExpr { get;  set; }

        public string ToSQLString()
        {
            return SqlPreBuilder.SQL.GetSql(this, "[]");
        }
        public override string ToString()
        {
            return SqlPreBuilder.SQL.GetSql(this, "[]");
        }
        public SQLInfo()
        {
            this.ElementType = typeof(T);
            this.ParamExpr = Expression.Parameter(this.ElementType);
        }
    }
}