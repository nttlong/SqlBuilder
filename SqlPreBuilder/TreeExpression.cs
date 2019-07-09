namespace SqlPreBuilder
{
    public class TreeExpression
    {
        public string Op { get;  set; }
        public TreeExpression Left { get;  set; }
        public TreeExpression Right { get;  set; }
        public DataFieldInfo Field { get;  set; }
        public FieldValue Value { get;  set; }
        public string ToSQLString(string Quote)
        {
            if (Value != null)
            {
                return Value.ToSQLString();
            }
            if (Field != null)
            {
                return Field.ToSQLString(Quote);
            }
            else
            {
                return "(" + Left.ToSQLString(Quote) + ")" + Op + "(" + Right.ToSQLString(Quote) + ")";
            }
        }
        public override string ToString()
        {
            return this.ToSQLString("[]");
        }
    }
}