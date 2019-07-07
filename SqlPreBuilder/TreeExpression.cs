namespace SqlPreBuilder
{
    public class TreeExpression
    {
        public string Op { get; internal set; }
        public TreeExpression Left { get; internal set; }
        public TreeExpression Right { get; internal set; }
        public DataFieldInfo Field { get; internal set; }
        public FieldValue Value { get; internal set; }
    }
}