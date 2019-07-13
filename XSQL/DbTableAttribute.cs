using System;

namespace XSQL
{
    public class DbTableAttribute : Attribute
    {
        public string TableName { get; set; }

        public string SchemaName { get;  set; }

        public DbTableAttribute(string TableName)
        {
            this.TableName = TableName;
        }
        public DbTableAttribute(string Schema,string TableName)
        {
            this.TableName = TableName;
            this.SchemaName = Schema;
        }
    }
}