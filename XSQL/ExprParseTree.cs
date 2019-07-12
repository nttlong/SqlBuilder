using System.Collections.Generic;

namespace XSQL
{
    public class ExprParseTree
    {
        public string Type { get; internal set; }
        public string Operator { get; internal set; }
        public ExprParseTree Left { get; internal set; }
        public ExprParseTree Right { get; internal set; }
        public ExprParseTree Argument { get; internal set; }
        public bool Prefix { get; internal set; }
        public List<ExprParseTree> Body { get; internal set; }
        public ExprParseTree Test { get; internal set; }
        public ExprParseTree Consequent { get; internal set; }
        public ExprParseTree Alternate { get; internal set; }
        public object Value { get; internal set; }
        public int Prec { get; internal set; }
        public string Raw { get; internal set; }
        public bool Computed { get; internal set; }
        public ExprParseTree ObjNode { get; internal set; }
        public ExprParseTree Property { get; internal set; }
        public ExprParseTree Callee { get; internal set; }
        public List<ExprParseTree> Arguments { get; internal set; }
        public string Name { get; internal set; }
    }
}