using System.Collections.Generic;

namespace XSQL
{
    public class XSqlCommand
    {
        public string CommandText { get; internal set; }
        internal List<XSqlCommandParam> Params { get; set; }
    }
}