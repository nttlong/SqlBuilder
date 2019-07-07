using System.Linq.Expressions;
using System.Reflection;

namespace SqlPreBuilder
{
    public class DataFieldInfo
    {
        public ParameterExpression ParamExpr { get; internal set; }
        public string Schema { get; internal set; }
        public string Table { get; internal set; }
        public MemberInfo Property { get; internal set; }
        public string Name { get; internal set; }
        public TreeExpression Expr { get; internal set; }
    }
}