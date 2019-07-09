using System;

namespace SqlPreBuilder
{
    public class FieldValue
    {
        public object Val { get; set; }
        public Type DataType { get; set; }
        public string ToSQLString()
        {
            if (Val == null)
            {
                return string.Format("({0})(null)", DataType.FullName);
            }
            else
            {
                return string.Format("({0})({1})", DataType.FullName,Val);
            }
        }
    }
}