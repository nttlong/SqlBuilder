using System;
using System.Linq.Expressions;
using System.Reflection;

namespace XSQL
{
    public class MapFieldInfo: FieldExpr
    {
        public PropertyInfo Member { get;  set; }
        public string Alias { get; set; }
        public TreeExpr ExprField { get; internal set; }
        public ParameterExpression ParamExpr { get; internal set; }

        internal MapFieldInfo Clone()
        {
            return new MapFieldInfo
            {

                AliasName = this.AliasName,
                Schema = this.Schema,
                Name = this.Name,
                TableName = this.TableName,
                Member=this.Member,
                ExprField=(this.ExprField!=null)?this.ExprField.Clone():null
            };
        }

        public MapFieldInfo SetTableName(string TableName)
        {
            this.Schema = null;
            this.TableName = TableName;
            if (this.ExprField != null)
            {
                this.ExprField.SetTableName(TableName);
                
            }
            return this;
        }

        
    }
    public class FieldExpr
    {
        public string Name { get; set; }
        public string Schema { get; set; }
        public string TableName { get; set; }
        
        public string AliasName { get;  set; }
        [System.Diagnostics.DebuggerStepThrough]
        public string ToSQLString(string Quotes, string paramPrefix)
        {
            var ret = "";
            if (Name == null)
            {
                return string.Format(string.Format("{0}{{0}}{1}.{{1}}", Quotes[0], Quotes[1]), TableName, "*");
            }
            if (Schema != null)
            {
                ret = string.Format(string.Format("{0}{{0}}{1}.{0}{{1}}{1}.{0}{{2}}{1}", Quotes[0], Quotes[1]), Schema,TableName, Name);
            }
            else
            {
                ret= string.Format(string.Format("{0}{{0}}{1}.{0}{{1}}{1}", Quotes[0], Quotes[1]), TableName, Name);
            }
            if (AliasName != null)
            {
                ret = ret + " " + string.Format(string.Format("{0}{{0}}{1}", Quotes[0], Quotes[1]), AliasName);
            }
            return ret;
        }
        public override string ToString()
        {
            return this.ToSQLString("[]","@");
        }

        public FieldExpr Clone()
        {
            return new FieldExpr
            {
                
                AliasName=this.AliasName,
                Schema=this.Schema,
                Name=this.Name,
                TableName=this.TableName
            };
        }

        public FieldExpr ClearAliasName()
        {
            this.AliasName = null;
            return this;
        }
    }
}