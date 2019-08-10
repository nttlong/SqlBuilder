using System.Collections.Generic;

namespace XSQL
{
    public class XSqlCommand
    {
        public string CommandText { get;  set; }
        public List<XSqlCommandParam> Params { get; set; }
    }
}