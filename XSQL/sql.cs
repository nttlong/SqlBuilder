using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
namespace XSQL
{
    public class Sql<T>:BaseSql, IQueryable<T>, IOrderedQueryable<T>
    {
        Expression IQueryable.Expression => Expression.Constant(this);
        IQueryProvider IQueryable.Provider => new XSqlProvider(this);

        

        public IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException();
        }
        public Sql<T> Select(params string[] fields)
        {
            var ret = BaseSql.Clone<T, T>(this);
            ret.SelectedFields = new List<TreeExpr>();
            foreach (var F in fields)
            {

               
               
                var mp = this.MapFields.FirstOrDefault(p => p.Name.Equals(F, StringComparison.InvariantCultureIgnoreCase));
                if (mp==null)
                {
                    throw new Exception(string.Format("{0} was not found", F));
                }
                ret.SelectedFields.Add(new TreeExpr
                {

                });
            }
            return ret;
        }
        public Sql<T2> LeftJoin<T1, T2>(Sql<T1> sql, Expression<Func<T, T1, bool>> ExprJoin, Expression<Func<T, T1, T2>> Selector)
        {
            var ret = this.Join(sql, ExprJoin, Selector);
            ret.source.JoinType = "left";
            return ret;
        }
        public Sql<T2> RightJoin<T1, T2>(Sql<T1> sql, Expression<Func<T, T1, bool>> ExprJoin, Expression<Func<T, T1, T2>> Selector)
        {
            var ret = this.Join(sql, ExprJoin, Selector);
            ret.source.JoinType = "right";
            return ret;
        }
        public Sql<T2> Join<T1,T2>(Sql<T1> sql, Expression<Func<T, T1, bool>> ExprJoin, Expression<Func<T, T1, T2>> Selector)
        {
            if ((!this.IsSubQuery) && (!sql.IsSubQuery))
            {
                var ret = new Sql<T2>();
                
                ret.MapFields = new List<MapFieldInfo>();
                this.AliasCount++;
                sql.AliasCount++;
                ret.AliasCount++;
                var leftAlias = "l" + ret.AliasCount+""+(this.AliasCount);
                var rightAlias = "r" + ret.AliasCount + "" +  (sql.AliasCount );
               
                this.MapFields.ForEach(p =>
                {
                    var P = p.Clone();
                    if (!ExprCompiler.IsPrimitiveType(((PropertyInfo)p.Member).PropertyType))
                    {
                        P.Name = null;
                    }
                    P.SetTableName(leftAlias);
                    P.ParamExpr = ExprJoin.Parameters[0];
                    ret.MapFields.Add(P);
                });
                sql.MapFields.ForEach(p =>
                {
                    var P = p.Clone();
                    if (!ExprCompiler.IsPrimitiveType(((PropertyInfo)p.Member).PropertyType))
                    {
                        P.Name = null;
                    }
                    P.SetTableName(rightAlias);
                    P.ParamExpr = ExprJoin.Parameters[1];
                    ret.MapFields.Add(P);
                });
                if (Selector.Body is NewExpression)
                {
                    var nx = Selector.Body as NewExpression;
                    if (nx.Members != null)
                    {
                        nx.Members.ToList().ForEach(p =>
                        {
                            var fx = nx.Arguments[nx.Members.IndexOf(p)];
                            var mp = ret.MapFields.FirstOrDefault(x => x.Member == p && x.ParamExpr == fx);
                            if (mp == null)
                            {
                                mp = ret.MapFields.FirstOrDefault(x => x.Member == p);
                            }
                            ret.MapFields.Add(new MapFieldInfo()
                            {
                                Alias = (mp != null) ? mp.Alias : null,
                                AliasName = (mp != null) ? mp.AliasName : null,
                                ExprField = (mp != null) ? mp.ExprField.Clone() : null,
                                Member = p as PropertyInfo,
                                Name = (mp != null) ? mp.Name : p.Name
                            });
                        });
                    }
                    
                }
                var JoinExpr = ExprCompiler.Compile(ret, ExprJoin.Body, null);
                JoinExpr.ClearAliasName();
                ret.source = new ExprDataSource
                {
                    JoinType="inner",
                    JoinExpr = JoinExpr,
                    LeftSource = new ExprDataSource
                    {
                        Alias=leftAlias,
                        Schema=this.schema,
                        Table=this.table,
                        ParamExpr=Expression.Parameter(this.ElementType,"p"),
                        Source=(this.source!=null)?this.source.Clone():null
                    },
                    RightSource=new ExprDataSource
                    {
                        Alias=rightAlias,
                        Schema=sql.schema,
                        Table=sql.table,
                        ParamExpr = Expression.Parameter(sql.ElementType, "p"),
                        Source = (sql.source != null) ? sql.source.Clone() : null
                    }
                };
                if(Selector.Body is NewExpression)
                {
                    ret.SelectedFields = ExprCompiler.GetSelectedFieldsFromNewExpression(ret, (NewExpression)Selector.Body);
                }
                else if(Selector.Body is MemberInitExpression)
                {
                    ret.SelectedFields = ExprCompiler.GetSelectedFieldsFromMemberInitExpression(ret, (MemberInitExpression)Selector.Body);
                }
                else if(Selector.Body is ParameterExpression)
                {
                    if (ret.SelectedFields == null) ret.SelectedFields = new List<TreeExpr>();
                    if (Selector.Parameters[0] == Selector.Body)
                    {
                        ret.SelectedFields.Add(new TreeExpr
                        {
                            Field = new FieldExpr
                            {
                                TableName = leftAlias
                            }
                        });
                        
                    }
                    else if(Selector.Body==Selector.Parameters[1])
                    {
                        ret.SelectedFields.Add(new TreeExpr
                        {
                            Field = new FieldExpr
                            {
                                TableName = rightAlias
                            }
                        });
                    }
                }
                else
                {
                    throw new NotSupportedException();
                }
                if (this.filter != null)
                {
                    ret.filter = this.filter.Clone();
                    ret.filter.SetTableName(leftAlias);
                }
                if (sql.filter != null)
                {
                    var rFilter = sql.filter.Clone();
                    rFilter.SetTableName(rightAlias);
                    sql.filter = new TreeExpr
                    {
                        Op = ExprCompiler.GetOp(ExpressionType.And),
                        Left = sql.filter,
                        Right = rFilter
                    };
                }
                ret.SelectedFields.Join(ret.MapFields, p => p.AliasName, q => q.Name, (p, q) => new { p, q })
                    .ToList().ForEach(F =>
                    {
                        F.q.ExprField = F.p.Clone();
                    });
                return ret;
            }
            throw new NotImplementedException();
        }
        public Sql<T3> Join<T2,TKey,T3>(Sql<T2> sql,Expression<Func<T,TKey>> LKey, Expression<Func<T2, TKey>> RKey,Expression<Func<T,T2,T3>> Selector)
        {
            var ExprJon = Expression<Func<T, T2, bool>>.Equal(LKey.Body, RKey.Body);
           
            var X = Expression.Lambda<Func<T, T2, bool>>(ExprJon, LKey.Parameters[0], RKey.Parameters[0]);
            return this.Join(sql, X, Selector);
           
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
        [System.Diagnostics.DebuggerStepThrough()]
        public Sql()
        {
            var attr = typeof(T).GetCustomAttributes().FirstOrDefault(p => p is DbTableAttribute) as DbTableAttribute;
            if (attr != null)
            {
                this.table = attr.TableName;
                this.schema = attr.SchemaName;
            }
            else
            {
                throw new Exception(string.Format("DbTable was not found in {0}", typeof(T)));
            }
            base.ElementType = typeof(T);
            base.ParamExpr = Expression.Parameter(base.ElementType);
            base.MapFields = this.ElementType.GetProperties().Select(p=>new MapFieldInfo {
                Member=p,
                Name=p.Name,
                Schema=this.schema,
                TableName=this.table
            }).ToList();
        }
        internal Sql(bool IgnoreCheck)
        {
            
            base.ElementType = typeof(T);
            base.ParamExpr = Expression.Parameter(base.ElementType);
            base.MapFields = this.ElementType.GetProperties().Select(p => new MapFieldInfo
            {
                Member = p
            }).ToList();
        }
        public Sql<T2> Select<T2>(Expression<Func<T, T2>> Expr)
        {
            if(Expr.Body is NewExpression)
            {
                var ret = BaseSql.Clone<T,T2>(this);
                if (ret.MapFields == null)
                {
                    ret.MapFields = new List<MapFieldInfo>();
                }
                ret.MapFields.AddRange(this.MapFields.Select(p=>p.Clone()));
                ret.SelectedFields = ExprCompiler.GetSelectedFieldsFromNewExpression(ret, (NewExpression)Expr.Body);
                var nx = Expr.Body as NewExpression;
                nx.Members.ToList().ForEach(p =>
                {
                    var mp = ret.MapFields.FirstOrDefault(x => x.Member == p);
                    ret.MapFields.Add(new MapFieldInfo()
                    {
                        Alias=(mp!=null)?mp.Alias:null,
                        AliasName = (mp != null) ? mp.AliasName : null,
                        ExprField = (mp != null) ? mp.ExprField.Clone() : null,
                        Member=p as PropertyInfo,
                        Name=p.Name,
                        ParamExpr=Expr.Parameters[0],
                        Schema=this.schema,
                        TableName=this.table
                    });
                });
                ret.SelectedFields.ForEach(p => {
                    if (p.Field != null)
                    {
                        if (p.Field.Name == p.Field.AliasName)
                        {
                            p.Field.AliasName = null;
                        }
                        else
                        {
                            //ret.IsSubQuery = true;
                        }
                    }
                    else if (p.Op != null)
                    {
                        //ret.IsSubQuery = true;
                    }
                });
                ret.SelectedFields.Join(ret.MapFields, p => p.AliasName, q => q.Name, (p, q) => new { p, q })
                    .ToList().ForEach(F =>
                    {
                        F.q.ExprField = F.p.Clone();
                    });
                return ret;
            }
            if(Expr.Body is MemberExpression)
            {
                var ret = BaseSql.Clone<T, T2>(this);
                var mp = ret.MapFields.FirstOrDefault(p => p.Member == ((MemberExpression)Expr.Body).Member);
                if (ret.SelectedFields == null)
                {
                    ret.SelectedFields = new List<TreeExpr>();
                }
                ret.SelectedFields.Add(new TreeExpr
                {
                    Field= new FieldExpr
                    {
                        AliasName=mp.AliasName,
                        Name=mp.Name,
                        Schema=mp.Schema,
                        TableName=mp.TableName
                    }
                });
                ret.MapFields.Clear();
                ret.MapFields.Add(mp);
                return ret;
            }
            if(Expr.Body is MemberInitExpression)
            {
                var mx = Expr.Body as MemberInitExpression;
                var ret = BaseSql.Clone<T, T2>(this);
                ret.SelectedFields = ExprCompiler.GetSelectedFieldsFromMemberInitExpression(ret, mx);
                ret.SelectedFields.ForEach(p => {
                    if (p.Field != null)
                    {
                        if (p.Field.Name == p.Field.AliasName)
                        {
                            p.Field.AliasName = null;
                        }
                        else
                        {
                            ret.IsSubQuery = true;
                        }
                    }
                    //else if (p.Op != null)
                    //{
                    //    ret.IsSubQuery = true;
                    //}
                });
                return ret;
            }
            if (Expr.Body is ParameterExpression)
            {
                var px = Expr.Body as ParameterExpression;
                var ret = BaseSql.Clone<T, T2>(this);
                var mp = ret.MapFields.FirstOrDefault(p => (p.ParamExpr != null) && p.ParamExpr.Type == px.Type);
                if (mp != null)
                {
                    ret.SelectedFields.Add(new TreeExpr()
                    {
                        Field=new FieldExpr
                        {
                            TableName=mp.TableName
                        }
                    });
                    ret.MapFields.AddRange(px.Type.GetProperties().Select(p => new MapFieldInfo()
                    {
                        Member=p,
                        Name=p.Name,
                        Schema=mp.Schema,
                        TableName=mp.TableName,
                        ParamExpr=px
                    }));
                }
                return ret;

            }
            throw new NotImplementedException();
        }
        public Sql<T> Where(Expression<Func<T, bool>> Expr)
        {
            Sql<T> ret = BaseSql.Clone<T, T>(this);

            if (!this.IsSubQuery)
            {
                
                if (this.SelectedFields != null && this.SelectedFields.Count > 0)
                {
                    if (ret.SelectedFields == null) ret.SelectedFields = new List<TreeExpr>();
                    ret.SelectedFields.AddRange(this.SelectedFields.Select(p => p.Clone()));
                    
                }
                if (((BaseSql)ret).filter == null)
                {
                    ((BaseSql)ret).filter = ExprCompiler.Compile(((BaseSql)ret), Expr.Body,null);
                }
                else
                {
                    ((BaseSql)ret).filter = new TreeExpr
                    {
                        Op=ExprCompiler.GetOp(ExpressionType.And),
                        Left= ((BaseSql)ret).filter,
                        Right= ExprCompiler.Compile(((BaseSql)ret), Expr.Body, ((MemberExpression)Expr.Body).Member)
                    };
                }
                return ret;
            }
            else
            {
                ret.AliasCount = this.AliasCount + 1;
                ret.Alias = "sql" + ret.AliasCount;
                ret.MapFields.ForEach(p => {
                    if (p.TableName == null)
                    {
                        p.TableName = ret.Alias;
                    }
                });
                ret.source = new ExprDataSource
                {
                    Alias = ret.Alias,
                    Fields=this.SelectedFields.Select(p=>p.Clone()).ToList(),
                    Schema=this.schema,
                    Table=this.table,
                    Source=this.source,
                    ParamExpr = Expression.Parameter(ret.ElementType, "p")

                };
                if (this.filter != null)
                {
                    ret.source.filter = this.filter.Clone();
                }
                ret.filter= ExprCompiler.Compile(((BaseSql)ret), Expr.Body, null); 
                return ret;
            }
        }
        public Sql(string schema, string table)
        {
            base.schema = schema;
            base.table = table;
            base.ElementType = typeof(T);
            base.ParamExpr = Expression.Parameter(base.ElementType);
            base.MapFields = this.ElementType.GetProperties().Select(p => new MapFieldInfo
            {
                Member = p
            }).ToList();
        }
        
    }
}
