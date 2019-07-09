using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace SqlPreBuilder
{
    public class Compiler
    {
        public static TreeExpression Compile<T>(SQLInfo<T> sql, Expression Expr)
        {
            var ret = new TreeExpression();
            if(Expr is BinaryExpression)
            {
                ret.Op = Utils.GetOp(Expr.NodeType);
                ret.Left = Compile(sql, ((BinaryExpression)Expr).Left);
                ret.Right = Compile(sql, ((BinaryExpression)Expr).Right);
                return ret;
            }
            if(Expr is MemberExpression)
            {
                
                return ComplieWithMemberExpression(sql, (MemberExpression)Expr);
            }
            if(Expr is ConstantExpression)
            {
                return CompileWitConstantEpxression(sql, (ConstantExpression)Expr);
            }
            if (Expr is UnaryExpression)
            {
                return Compile(sql, ((UnaryExpression)Expr).Operand);
            }
            throw new NotImplementedException();
        }

        public static TreeExpression CompileWitConstantEpxression<T>(SQLInfo<T> sql, ConstantExpression expr)
        {
            return new TreeExpression
            {
                Value = new FieldValue {
                    Val = Expression.Lambda(expr).Compile().DynamicInvoke(),
                    DataType=expr.Type
                }
            };
        }

        public static TreeExpression ComplieWithMemberExpression<T>(SQLInfo<T> sql, MemberExpression expr)
        {
            var field = sql.Fields.FirstOrDefault(p => p.ParamExpr == expr.Expression && p.Property == expr.Member);
            if (field == null)
            {
                field = sql.Fields.FirstOrDefault(p => p.ParamExpr.Type == expr.Expression.Type && p.Property.Name == expr.Member.Name);
            }
            if (field == null) {
                field = sql.Fields.FirstOrDefault(p =>  p.Property.Name == expr.Member.Name);
            }
            if (field != null)
            {
                return new TreeExpression
                {
                    Field=new DataFieldInfo()
                    {
                        ParamExpr=Expression.Parameter(typeof(T),"p"),
                        Name=expr.Member.Name,
                        Property=expr.Member,
                        Schema=sql.SchemaName,
                        Table=sql.TableName
                    }
                };
            }
            else
            {
                throw new Exception(string.Format("'{0}' was not found", expr.Member.Name));
            }
            throw new NotImplementedException();
        }
    }
}
