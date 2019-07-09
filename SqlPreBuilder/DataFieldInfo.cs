using System.Linq.Expressions;
using System.Reflection;

namespace SqlPreBuilder
{
    public class DataFieldInfo
    {
        public ParameterExpression ParamExpr { get;  set; }
        public string Schema { get;  set; }
        public string Table { get;  set; }
        public MemberInfo Property { get;  set; }
        public string Name { get;  set; }
        public TreeExpression Expr { get;  set; }
        public string ToSQLString(string Quote)
        {
            if (Expr == null)
            {
                return string.Format("{0}.{1}.{2}", Schema, Table, Name);
            }
            else
            {
                return string.Format("{0} {1}",Expr.ToSQLString(Quote),Name);
            }
        }
        public override string ToString()
        {
            return this.ToSQLString("[]");
        }

    }
}