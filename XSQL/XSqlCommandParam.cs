using System;

namespace XSQL
{
    public class XSqlCommandParam
    {
        public Type DataType { get;  set; }
        public object Value { get;  set; }
        public string Name { get;  set; }
    }
}