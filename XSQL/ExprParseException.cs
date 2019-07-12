using System;
using System.Runtime.Serialization;

namespace XSQL
{
    [Serializable]
    internal class ExprParseException : Exception
    {
        private object p;

        public ExprParseException()
        {
        }
        public ExprParseException(string msg,int index):base(msg)
        {
            this.index = index;
        }
        public ExprParseException(object p)
        {
            this.p = p;
        }

        public ExprParseException(string message) : base(message)
        {
        }

        public ExprParseException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ExprParseException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public int index { get; internal set; }
        public string description { get; internal set; }
    }
}