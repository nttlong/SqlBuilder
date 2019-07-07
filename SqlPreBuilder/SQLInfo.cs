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
        public List<DataFieldInfo> SelectFields { get; internal set; }
    }
}