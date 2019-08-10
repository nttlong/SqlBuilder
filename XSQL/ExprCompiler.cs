using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace XSQL
{
    public class ExprCompiler
    {
        public static string currentParamName;

        public static TreeExpr Compile(BaseSql sql, Expression Expr, MemberInfo MB)
        {
            if (Expr is BinaryExpression)
            {
                return CompileBin(sql, (BinaryExpression)Expr, MB);
            }
            if (Expr is MemberExpression)
            {
                return CompileMember(sql, (MemberExpression)Expr, MB);
            }
            if (Expr is ConstantExpression)
            {
                return CompileConst(sql, (ConstantExpression)Expr,MB);
            }
            if (Expr is ParameterExpression)
            {
                return CompileParaExpr(sql, (ParameterExpression)Expr);
            }
            if (Expr is UnaryExpression)
            {
                return Compile(sql, ((UnaryExpression)Expr).Operand, MB);
            }
            if (Expr is LambdaExpression)
            {
                return Compile(sql, ((LambdaExpression)Expr).Body, MB);
            }
            if(Expr is MethodCallExpression)
            {
                return CompileMethodCallExpression(sql, (MethodCallExpression)Expr, MB);
            }

            throw new NotImplementedException();
        }

        public static TreeExpr CompileMethodCallExpression(BaseSql sql, MethodCallExpression expr, MemberInfo mB)
        {
            if (expr.Method.DeclaringType == typeof(SqlFn))
            {
                if(expr.Arguments.Count==1 && expr.Arguments[0] is NewArrayExpression)
                {
                    var args = expr.Arguments[0] as NewArrayExpression;
                    var tmpArgs = args.Expressions.Select(p => Compile(sql, p, mB)).ToList();
                    tmpArgs.ForEach(p =>
                    {
                        p.ClearAliasName();
                    });
                    return new TreeExpr
                    {
                        Callee = new FuncExpr
                        {
                            Name = expr.Method.Name,
                            Arguments = tmpArgs,
                            AliasName = (mB != null) ? mB.Name:null
                        },
                        AliasName=(mB!=null)?mB.Name:null
                        
                    };
                }
                else
                {
                    var tmpArgs = expr.Arguments.Select(p => Compile(sql, p, mB)).ToList();
                    tmpArgs.ForEach(p =>
                    {
                        p.ClearAliasName();
                    });
                    return new TreeExpr
                    {
                        Callee = new FuncExpr
                        {
                            Name = expr.Method.Name,
                            Arguments = tmpArgs,
                            AliasName = (mB != null) ? mB.Name : null
                        },
                        AliasName = mB.Name
                    };
                }
                
            }
            throw new NotImplementedException();
        }

        internal static Sql<T> DoGroupBy<T>(MethodCallExpression expr)
        {
            
            var sql = Expression.Lambda(expr.Arguments[0]).Compile().DynamicInvoke() as BaseSql;
            var ret = BaseSql.Clone<T>(sql);
            ret.MapFields.AddRange(sql.MapFields.Select(p => p.Clone()));
            var ExprGroupBy = expr.Arguments[1];
            ret.GroupByFields = ExprCompiler.GetSelectedFieldsFromExpression(ret, ExprGroupBy);
            ret.IsSubQuery = true;
            var mbxs = ExprCompiler.GetAllMemberExpression(expr);
            typeof(T).GetProperties().ToList().ForEach(p =>
            {
                ret.MapFields.Add(new MapFieldInfo
                {
                    Member=p,
                    ParamExpr=Expression.Parameter(p.PropertyType,"p"),
                    Name=p.Name
                });
            });
            return ret;
        }

        public static List<TreeExpr> GetSelectedFieldsFromExpression<T>(Sql<T> sql, Expression expr)
        {
            if(expr is NewExpression)
            {
                return GetSelectedFieldsFromNewExpression(sql, (NewExpression)expr);
            }
            else if(expr is MemberInitExpression)
            {
                return GetSelectedFieldsFromMemberInitExpression(sql, (MemberInitExpression)expr);
            }
            else if(expr is MemberExpression)
            {
                return GetSelectedFieldsFromMemberExpression(sql, (MemberExpression)expr);
            }
            else if(expr is UnaryExpression)
            {
                return GetSelectedFieldsFromExpression(sql, ((UnaryExpression)expr).Operand);
            }
            else if(expr is LambdaExpression)
            {
                return GetSelectedFieldsFromLambdaExpression(sql, ((LambdaExpression)expr));
            }
            throw new NotImplementedException();
        }

        public static List<TreeExpr> GetSelectedFieldsFromLambdaExpression<T>(Sql<T> sql, LambdaExpression expr)
        {
            return GetSelectedFieldsFromExpression(sql, expr.Body);
            
            throw new NotImplementedException();
        }

        public static List<TreeExpr> GetSelectedFieldsFromMemberExpression<T>(Sql<T> sql, MemberExpression expr)
        {
            var mp = sql.MapFields.FirstOrDefault(p => p.Member == expr.Member);
            var ret = new List<TreeExpr>();
            ret.Add(new TreeExpr
            {
                Field = new FieldExpr
                {
                    AliasName = (mp != null) ? mp.AliasName : null,
                    Name = expr.Member.Name,
                    Schema = (mp != null) ? mp.Schema : sql.schema,
                    TableName=(mp!=null)?mp.TableName:sql.table
                    
                }
            }) ;
            return ret;
            
        }

        public static TreeExpr CompileParaExpr(BaseSql sql, ParameterExpression expr)
        {
            if (sql.source != null)
            {
                if (sql.source.LeftSource != null && sql.source.LeftSource.ParamExpr.Type == expr.Type)
                {
                    return new TreeExpr
                    {
                        Field = new FieldExpr
                        {
                            TableName = sql.source.LeftSource.Table
                        }
                    };

                }
                if (sql.source.LeftSource != null && sql.source.RightSource.ParamExpr.Type == expr.Type)
                {
                    return new TreeExpr
                    {
                        Field = new FieldExpr
                        {
                            TableName = sql.source.RightSource.Table
                        }
                    };
                }
            }
            else
            {
                var mp = sql.MapFields.FirstOrDefault(p => p.ParamExpr == expr);
                if (mp != null)
                {
                    return new TreeExpr
                    {
                        Field = new FieldExpr
                        {
                            TableName = mp.TableName,
                            Schema = mp.Schema
                        }
                    };
                }
                if (expr.Type.Name.IndexOf("IGrouping") > -1)
                {
                    
                }
                
            }
            throw new NotImplementedException();
        }

        public static IQueryable<T> DoInnerJoin<T>(BaseSql qr1, BaseSql qr2, MemberExpression leftKey, MemberExpression rightKey, Expression selector)
        {
            var ret = new Sql<T>(true);
            var leftName = "l" + ret.AliasCount + "" + qr1.AliasCount + "" + qr2.AliasCount;
            var rightName = "r" + ret.AliasCount + "" + qr1.AliasCount + "" + qr2.AliasCount;
            if (!(qr1.IsSubQuery) && (!qr2.IsSubQuery))
            {

                var leftMp = qr1.MapFields.FirstOrDefault(p => p.Member == leftKey.Member);
                var rightMp = qr2.MapFields.FirstOrDefault(p => p.Member == rightKey.Member);
                ret.source = new ExprDataSource
                {
                    JoinType = "inner",
                    LeftSource = new ExprDataSource
                    {
                        Alias = leftName,
                        Schema = qr1.schema,
                        Table = qr1.table
                    },
                    RightSource = new ExprDataSource
                    {
                        Alias = rightName,
                        Schema = qr2.schema,
                        Table = qr2.table
                    },
                    JoinExpr = new TreeExpr
                    {
                        Op = ExprCompiler.GetOp(ExpressionType.Equal),
                        Left = new TreeExpr
                        {
                            Field = new FieldExpr
                            {
                                TableName = leftName,
                                Name = leftMp.Name
                            }
                        },
                        Right = new TreeExpr
                        {
                            Field = new FieldExpr
                            {
                                TableName = rightName,
                                Name = rightMp.Name
                            }
                        }

                    }
                };
                return ret as IQueryable<T>;
            }
            if ((qr1.IsSubQuery) && (!qr2.IsSubQuery))
            {
                var sql1 =  new BaseSql();
                var leftMp = qr1.MapFields.FirstOrDefault(p => p.Member == leftKey.Member);
                var rightMp = qr2.MapFields.FirstOrDefault(p => p.Member == rightKey.Member);
                ret.source = new ExprDataSource
                {
                    JoinType = "inner",
                    LeftSource = new ExprDataSource
                    {
                        Source=new ExprDataSource
                        {
                            Alias=qr1.Alias,
                            Fields=(qr1.SelectedFields!=null)?qr1.SelectedFields.Select(p=> p.Clone()).ToList():null,
                            filter=(qr1.filter!=null)?qr1.filter.Clone():null,
                            GroupFields= (qr1.GroupByFields != null)?qr1.GroupByFields.Select(p=>p.Clone()).ToList():null,
                            HavingFields=(qr1.HavingFields!=null)?qr1.HavingFields.Select(p=>p.Clone()).ToList():null,
                            Schema=qr1.schema,
                            Table=qr1.table,
                            JoinExpr=(qr1.source!=null)?qr1.source.JoinExpr:null,
                            JoinType = (qr1.source != null) ? qr1.source.JoinType : null,
                            LeftSource = (qr1.source != null) ? qr1.source.LeftSource : null,
                            RightSource = (qr1.source != null) ? qr1.source.RightSource : null,
                            ParamExpr=qr1.ParamExpr
                        }
                    },
                    RightSource = new ExprDataSource
                    {
                        Alias = rightName,
                        Schema = qr2.schema,
                        Table = qr2.table
                    },
                    JoinExpr = new TreeExpr
                    {
                        Op = ExprCompiler.GetOp(ExpressionType.Equal),
                        Left = new TreeExpr
                        {
                            Field = new FieldExpr
                            {
                                TableName = leftName,
                                Name = leftMp.Name
                            }
                        },
                        Right = new TreeExpr
                        {
                            Field = new FieldExpr
                            {
                                TableName = rightName,
                                Name = rightMp.Name
                            }
                        }

                    }
                };
                if(ret.source.LeftSource.Fields==null ||
                    ret.source.Fields.Count == 0)
                {
                    ret.source.Fields = new List<TreeExpr>();
                    ret.source.Fields.Add(new TreeExpr
                    {
                       Field=new FieldExpr
                       {
                          TableName=qr1.table 
                       } 
                    });
                }
                return ret as IQueryable<T>;

            }
            throw new NotSupportedException();
        }

        public static IQueryable<T> DoSelectMany<T>(MethodCallExpression Expr)
        {
            if (Expr.Arguments[0].Type.BaseType == typeof(BaseSql))
            {
                var qr = Expression.Lambda(Expr.Arguments[0]).Compile().DynamicInvoke() as BaseSql;
                var secondParam = ((LambdaExpression)((UnaryExpression)Expr.Arguments[1]).Operand).Body;
                if (secondParam is MemberExpression)
                {
                    var fx = (MemberExpression)secondParam;
                    var qr2 = (BaseSql)Expression.Lambda(fx).Compile().DynamicInvoke();
                    return CrossJoin<T>(qr, qr2, (LambdaExpression)((UnaryExpression)Expr.Arguments[2]).Operand);
                }
                else if (secondParam is MethodCallExpression)
                {
                    var cx = secondParam as MethodCallExpression;
                    if (cx.Method.Name == "Where")
                    {
                        var qr2 = Expression.Lambda(cx.Arguments[0]).Compile().DynamicInvoke() as BaseSql;
                        var conditional = ((LambdaExpression)((UnaryExpression)cx.Arguments[1]).Operand);
                        var selector = ((LambdaExpression)((UnaryExpression)Expr.Arguments[2]).Operand);
                        return DoInnerJoinByLambdaExpression<T>(qr, qr2, conditional, selector);
                    }
                    if (cx.Method.Name == "DefaultIfEmpty")
                    {
                        if (((MethodCallExpression)cx.Arguments[0]).Method.Name == "Where")
                        {
                            BaseSql qr2 = null;
                            LambdaExpression conditional = null;
                            var cxChild = (MethodCallExpression)cx.Arguments[0];
                            if (cxChild.Object == null)
                            {
                                qr2 = Expression.Lambda(cxChild.Arguments[0]).Compile().DynamicInvoke() as BaseSql;
                                conditional = ((LambdaExpression)((UnaryExpression)cxChild.Arguments[1]).Operand);
                            }
                            else
                            {
                                qr2 = Expression.Lambda(cxChild.Object).Compile().DynamicInvoke() as BaseSql;
                                conditional = ((LambdaExpression)((UnaryExpression)cxChild.Arguments[0]).Operand);
                            }

                            var selector = ((LambdaExpression)((UnaryExpression)Expr.Arguments[2]).Operand);
                            var ret = DoInnerJoinByLambdaExpression<T>(qr, qr2, conditional, selector);
                            return ret;
                        }
                    }
                }
            }
            throw new NotImplementedException();
        }
        internal static List<ParameterExpression> GetAllParamsExpr(Expression expr)
        {
            var ret = new List<ParameterExpression>();
            if (expr is LambdaExpression)
            {
                ret.AddRange(GetAllParamsExpr(((LambdaExpression)expr).Body));
            }
            else if (expr is BinaryExpression)
            {
                var bx = expr as BinaryExpression;
                ret.AddRange(GetAllParamsExpr(bx.Left));
                ret.AddRange(GetAllParamsExpr(bx.Right));
            }
            else if (expr is MemberExpression)
            {
                var mb = expr as MemberExpression;
                ret.AddRange(GetAllParamsExpr(mb.Expression));
            }
            else if (expr is ParameterExpression)
            {
                ret.Add((ParameterExpression)expr);
            }
            else if (expr is MethodCallExpression)
            {
                var mc = expr as MethodCallExpression;
                foreach (var x in mc.Arguments)
                {
                    ret.AddRange(GetAllParamsExpr(x));
                }
            }
            else if (expr is UnaryExpression)
            {
                var ux = expr as UnaryExpression;
                ret.AddRange(GetAllParamsExpr(ux.Operand));

            }
            else if (expr is NewArrayExpression)
            {
                var nxa = expr as NewArrayExpression;
                foreach (var x in nxa.Expressions)
                {
                    ret.AddRange(GetAllParamsExpr(x));
                }
            }
            else if (expr is ConstantExpression)
            {
                return ret;
            }
            else
            {
                throw new NotImplementedException();
            }
            return ret;
        }
        internal static List<MemberExpression> GetAllMemberExpression(Expression expr)
        {
            var ret = new List<MemberExpression>();
            if (expr is UnaryExpression)
            {
                var ux = expr as UnaryExpression;
                var tmp = GetAllMemberExpression(ux.Operand);
                ret.AddRange(tmp);
            }
            if (expr is MemberExpression)
            {
                ret.Add((MemberExpression)expr);
                ret.AddRange(GetAllMemberExpression(((MemberExpression)expr).Expression));
            }
            else if (expr is MethodCallExpression)
            {
                foreach (var arg in ((MethodCallExpression)expr).Arguments)
                {
                    ret.AddRange(GetAllMemberExpression(arg));
                }
            }
            else if (expr is LambdaExpression)
            {
                ret.AddRange(GetAllMemberExpression(((LambdaExpression)expr).Body));
            }
            else if (expr is BinaryExpression)
            {
                ret.AddRange(GetAllMemberExpression(((BinaryExpression)expr).Left));
                ret.AddRange(GetAllMemberExpression(((BinaryExpression)expr).Right));
            }
            else if (expr is ParameterExpression)
            {
                return ret;
            }
            else if (expr is UnaryExpression)
            {
                ret.AddRange(GetAllMemberExpression(((UnaryExpression)expr).Operand));
            }
            else if (expr is ConstantExpression)
            {
                return ret;
            }
            else if (expr is NewArrayExpression)
            {
                var nx = expr as NewArrayExpression;
                nx.Expressions.ToList().ForEach(p =>
                {
                    ret.AddRange(GetAllMemberExpression(p));
                });
            }
            else if (expr is NewExpression)
            {
                var nx = expr as NewExpression;
                nx.Arguments.ToList().ForEach(p =>
                {
                    ret.AddRange(GetAllMemberExpression(p));
                });
            }
            else if(expr is MemberInitExpression)
            {
                
                var nx = expr as MemberInitExpression;
                foreach(var x in nx.Bindings)
                {
                    if(x is MemberAssignment)
                    {
                        
                        ret.AddRange(GetAllMemberExpression(((MemberAssignment)x).Expression));

                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
               
            }
            else
            {
                throw new NotImplementedException();
            }
            ret = ret.Distinct().ToList();
            return ret;
        }
        public static Sql<T> DoInnerJoinByLambdaExpression<T>(BaseSql qr1, BaseSql qr2, LambdaExpression conditional, LambdaExpression selector)
        {
            var x = GetAllParamsExpr(conditional);
            //var y = GetAllParamsExpr(selector);
            var ret = new Sql<T>(true);
            ret.AliasCount++;
            var leftAlias = "l" + ret.AliasCount + "" + qr1.AliasCount + "" + qr2.AliasCount;
            var righttAlias = "r" + ret.AliasCount + "" + qr1.AliasCount + "" + qr2.AliasCount;

            ret.MapFields = new List<MapFieldInfo>();
            if (qr1.MapFields != null)
            {
                qr1.MapFields.ForEach(p =>
                {
                    var P = p.Clone();

                    P.SetTableName(leftAlias);
                    ret.MapFields.Add(P);
                    P.ParamExpr = selector.Parameters[0];
                    var P1 = P.Clone();
                    P1.ParamExpr = x[0];
                    ret.MapFields.Add(P1);
                });

            }
            if (qr2.MapFields != null)
            {
                qr2.MapFields.ForEach(p =>
                {
                    var P = p.Clone();
                    P.SetTableName(righttAlias);
                    ret.MapFields.Add(P);
                    P.ParamExpr = selector.Parameters[1];
                    var P1 = P.Clone();
                    P1.ParamExpr = x[1];
                    ret.MapFields.Add(P1);
                });
            }
            var joinExor = ExprCompiler.Compile(ret, conditional, null);
            if (selector.Body is MemberInitExpression)
            {
                ret.SelectedFields = ExprCompiler.GetSelectedFieldsFromMemberInitExpression(ret, (MemberInitExpression)selector.Body);
            }
            if (selector.Body is NewExpression)
            {
                ret.SelectedFields = ExprCompiler.GetSelectedFieldsFromNewExpression(ret, (NewExpression)selector.Body);
            }
            ret.source = new ExprDataSource
            {
                JoinExpr = joinExor,
                JoinType = "left"
            };
            ret.source.LeftSource = new ExprDataSource
            {
                Schema = qr1.schema,
                Table = qr1.table,
                Alias = leftAlias,
                Source = (qr1.source != null) ? qr1.source.Clone() : null
            };
            ret.source.RightSource = new ExprDataSource
            {
                Schema = qr2.schema,
                Table = qr2.table,
                Alias = righttAlias,
                Source = (qr2.source != null) ? qr2.source.Clone() : null
            };

            if (selector.Body is NewExpression)
            {
                var nx = selector.Body as NewExpression;
                foreach (var mb in nx.Members)
                {
                    if(!(nx.Arguments[nx.Members.IndexOf(mb)] is ParameterExpression))
                    {
                        var fm = ret.SelectedFields.FirstOrDefault(p => (p.Field != null) && (p.Field.Name == mb.Name));
                        if (fm == null)
                        {
                            fm = ret.SelectedFields.FirstOrDefault(p => (p.Value != null) && (p.Value.AliasName == mb.Name));
                        }
                        ret.MapFields.Add(new MapFieldInfo()
                        {
                            Member=mb as PropertyInfo,
                            ParamExpr=Expression.Parameter(typeof(T),"p"),
                            Name=mb.Name,
                            Schema=ret.schema,
                            TableName=ret.table,
                            ExprField=fm
                            
                        });
                    }
                    
                }
            }
            else if(selector.Body is MemberInitExpression)
            {
                var mbi = selector.Body as MemberInitExpression;
                foreach(MemberAssignment mba in mbi.Bindings)
                {
                    if(!(mba.Expression is ParameterExpression))
                    {
                        var fm = ret.SelectedFields.FirstOrDefault(p => (p.Field != null) && (p.Field.Name == mba.Member.Name));
                        if (fm == null)
                        {
                            fm = ret.SelectedFields.FirstOrDefault(p => (p.Value != null) && (p.Value.AliasName == mba.Member.Name));
                        }
                        ret.MapFields.Add(new MapFieldInfo()
                        {
                            Member = mba.Member as PropertyInfo,
                            ParamExpr = Expression.Parameter(typeof(T), "p"),
                            Name = mba.Member.Name,
                            Schema = ret.schema,
                            TableName = ret.table,
                            ExprField = fm

                        });
                    }
                }
            }
            else
            {
                throw new NotImplementedException();
            }
            return ret;
        }

        public static IQueryable<T> DoSelectByMethodCallExpression<T>(MethodCallExpression cx)
        {
            if (cx.Arguments[0].Type.BaseType == typeof(BaseSql))
            {
                var selector = ((LambdaExpression)((UnaryExpression)cx.Arguments[1]).Operand).Body;
                var qr = Expression.Lambda(cx.Arguments[0]).Compile().DynamicInvoke() as BaseSql;
                if (selector is NewExpression)
                {
                    return DoSelectByNewExpression<T>(qr, (NewExpression)selector);
                }
                else if (selector is MemberExpression)
                {
                    return DoSelectByMemberExpression<T>(qr, (MemberExpression)selector);
                }
                else if(selector is UnaryExpression)
                {
                    return DoSelectByExpression<T>(qr, ((UnaryExpression)selector).Operand);
                }
                else if(selector is MethodCallExpression)
                {
                    return DoSelectByMethodCallExpression<T>(qr, (MethodCallExpression)selector);
                }
                else if(selector is MemberInitExpression)
                {
                    return DoSelectByMMemberInitExpression<T>(qr, (MemberInitExpression)selector);
                }
                else if(selector is ParameterExpression)
                {
                    return DoSelectByParameterExpression<T>(qr, (ParameterExpression)selector);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            throw new NotImplementedException();
        }

        public static IQueryable<T> DoSelectByParameterExpression<T>(BaseSql qr, ParameterExpression selector)
        {
            var ret = BaseSql.Clone<T>(qr);
            if (selector.Type == typeof(T))
            {
                return ret;    
            }
            throw new NotImplementedException();
        }

        public static IQueryable<T> DoSelectByMMemberInitExpression<T>(BaseSql qr, MemberInitExpression Expr)
        {
            var ret = BaseSql.Clone<T>(qr);
            ret.SelectedFields = new List<TreeExpr>();
            foreach(MemberAssignment mba in Expr.Bindings)
            {
                ret.SelectedFields.Add(ExprCompiler.Compile(ret, mba.Expression, mba.Member));
            }
            
            return ret;
        }

        public static IQueryable<T> DoSelectByMethodCallExpression<T>(BaseSql sql, MethodCallExpression expr)
        {
            var ret = BaseSql.Clone<T>(sql);
            if (expr.Method.DeclaringType == typeof(SqlFn))
            {
                if (expr.Arguments.Count == 1)
                {
                    if (ret.SelectedFields == null)
                    {
                        ret.SelectedFields = new List<TreeExpr>();
                    }
                    ret.SelectedFields.Add(new TreeExpr
                    {
                        Callee=new FuncExpr
                        {
                            Name=expr.Method.Name,
                            Arguments=expr.Arguments.Select(p=>ExprCompiler.Compile(ret,p,null)).ToList()
                        }
                    });
                    return ret as IQueryable<T>;
                }
            }
            else if(expr.Method.DeclaringType == typeof(Enumerable))
            {
                if (expr.Method.Name == "First")
                {

                }
            }
            throw new NotImplementedException();
        }

        public static IQueryable<T> DoSelectByExpression<T>(BaseSql qr, Expression Expr)
        {
            if(Expr is MemberExpression)
            {
                return DoSelectByMemberExpression<T>(qr,(MemberExpression)Expr);
            }
            throw new NotImplementedException();
        }

        public static IQueryable<T> DoSelectByMemberExpression<T>(BaseSql qr, MemberExpression selector)
        {
            var ret = BaseSql.Clone<T>(qr);
            ret.SelectedFields = GetSelectedFieldsFromMemberExpression(ret, selector);
            return ret as IQueryable<T>;
        }

        public static IQueryable<T> DoSelectByNewExpression<T>(BaseSql qr, NewExpression Expr)
        {

            var ret = BaseSql.Clone<T>(qr);
            ret.SelectedFields = Expr.Arguments.Select(p => ExprCompiler.Compile(qr, p, Expr.Members[Expr.Arguments.IndexOf(p)])).ToList();
            return ret as IQueryable<T>;

        }



        public static IQueryable<T> CrossJoin<T>(BaseSql qr1, BaseSql qr2, LambdaExpression Expr)
        {
            var ret = new Sql<T>();
            ret.MapFields = new List<MapFieldInfo>();
            ret.MapFields.AddRange(qr1.MapFields.Select(p => p.Clone()));
            ret.MapFields.AddRange(qr2.MapFields.Select(p => p.Clone()));
            ret.AliasCount++;
            if (Expr.Body is NewExpression)
            {
                ret.source = new ExprDataSource
                {
                    LeftSource = (qr1.source != null) ? qr1.source.Clone() : null,
                    RightSource = (qr2.source != null) ? qr2.source.Clone() : null,
                    ParamExpr = Expression.Parameter(ret.ElementType, "p")
                };
                if (ret.source.LeftSource == null)
                {
                    ret.source.LeftSource = new ExprDataSource
                    {
                        Table = qr1.table,
                        Schema = qr1.schema,
                        Alias = "l" + ret.AliasCount + "" + qr1.AliasCount + "" + qr2.AliasCount,
                        ParamExpr = Expression.Parameter(qr1.ElementType, "p")

                    };
                    if (qr1.filter != null)
                    {
                        ret.source.LeftSource.filter = qr1.filter.Clone();
                    }
                    if (qr1.SelectedFields != null)
                    {
                        ret.source.LeftSource.Fields = qr1.SelectedFields.Select(p => p.Clone()).ToList();
                    }
                }
                else
                {
                    ret.source.LeftSource.Alias = "l" + ret.AliasCount + "" + qr1.AliasCount + "" + qr2.AliasCount;
                }
                if (ret.source.RightSource == null)
                {
                    ret.source.RightSource = new ExprDataSource
                    {
                        Table = qr2.table,
                        Schema = qr2.schema,
                        Alias = "r" + ret.AliasCount + "" + qr1.AliasCount + "" + qr2.AliasCount,
                        ParamExpr = Expression.Parameter(qr2.ElementType, "p")

                    };
                    if (qr2.filter != null)
                    {
                        ret.source.LeftSource.filter = qr2.filter.Clone();
                    }
                    if (qr2.SelectedFields != null)
                    {
                        ret.source.LeftSource.Fields = qr2.SelectedFields.Select(p => p.Clone()).ToList();
                    }
                }
                else
                {
                    ret.source.RightSource.Alias = "r" + ret.AliasCount + "" + qr1.AliasCount + "" + qr2.AliasCount;
                }
                var nx = Expr.Body as NewExpression;
                foreach (var x in nx.Arguments)
                {
                    var isInLeft = true;
                    if (x is MemberExpression)
                    {
                        var mx = x as MemberExpression;
                        var mp = qr1.MapFields.FirstOrDefault(p => p.Member == mx.Member);
                        if (mp == null)
                        {
                            isInLeft = false;
                            mp = qr2.MapFields.FirstOrDefault(p => p.Member == mx.Member);
                        }
                        if (mp != null)
                        {
                            ret.MapFields.Add(new MapFieldInfo()
                            {
                                TableName = (isInLeft) ? ret.source.LeftSource.Alias : ret.source.RightSource.Alias,
                                AliasName = mp.AliasName,
                                Member = mp.Member,
                                Name = mp.Name

                            });
                        }
                        if (ret.SelectedFields == null)
                        {
                            ret.SelectedFields = new List<TreeExpr>();
                        }
                        ret.SelectedFields.Add(new TreeExpr
                        {
                            Field = new FieldExpr
                            {
                                TableName = (isInLeft) ? ret.source.LeftSource.Alias : ret.source.RightSource.Alias,
                                Name = mp.Name

                            }
                        });
                    }
                    else if (x is ParameterExpression)
                    {
                        if (ret.SelectedFields == null)
                        {
                            ret.SelectedFields = new List<TreeExpr>();
                        }
                        if (x == Expr.Parameters[0])
                        {
                            ret.SelectedFields.Add(new TreeExpr
                            {
                                Field = new FieldExpr
                                {
                                    TableName = ret.source.LeftSource.Alias
                                }
                            });
                            x.Type.GetProperties().ToList().ForEach(p =>
                            {
                                var mp = qr1.MapFields.FirstOrDefault(fx => fx.Member == p);
                                ret.MapFields.Add(new MapFieldInfo()
                                {
                                    Member = mp.Member,
                                    Name = mp.Name,
                                    TableName = ret.source.LeftSource.Alias

                                });

                            });
                            ret.MapFields.AddRange(x.Type.GetProperties().Select(p => new MapFieldInfo
                            {
                                TableName = ret.source.LeftSource.Alias,
                                Member = p as PropertyInfo

                            }).ToList());
                        }
                        else
                        {
                            x.Type.GetProperties().ToList().ForEach(p =>
                            {
                                var mp = qr2.MapFields.FirstOrDefault(fx => fx.Member == p);
                                ret.MapFields.Add(new MapFieldInfo()
                                {
                                    Member = mp.Member,
                                    Name = mp.Name,
                                    TableName = ret.source.RightSource.Alias

                                });

                            });
                            ret.SelectedFields.Add(new TreeExpr
                            {
                                Field = new FieldExpr
                                {
                                    TableName = ret.source.RightSource.Alias
                                }
                            });
                        }
                    }
                    else if (x is BinaryExpression)
                    {
                        if (ret.SelectedFields == null)
                        {
                            ret.SelectedFields = new List<TreeExpr>();
                        }
                        var F = Compile(ret, x, nx.Members[nx.Arguments.IndexOf(x)]);
                        ret.SelectedFields.Add(F);
                        ret.MapFields.Add(new MapFieldInfo
                        {
                            Alias = ret.Alias,
                            AliasName = ret.Alias,
                            Member = nx.Members[nx.Arguments.IndexOf(x)] as PropertyInfo,
                            Name = nx.Members[nx.Arguments.IndexOf(x)].Name,
                            Schema = ret.schema,
                            TableName = ret.table,
                            ExprField = F

                        });

                    }
                    else
                    {
                        throw new NotImplementedException();
                    }

                }
                return ret;
            }
            else
            {
                throw new NotImplementedException();
            }

        }

        public static List<TreeExpr> GetSelectedFieldsFromMemberInitExpression<T2>(Sql<T2> sql, MemberInitExpression Expr)
        {
            var ret = new List<TreeExpr>();
            foreach (MemberAssignment x in Expr.Bindings)
            {
                //var mb = Expr.Members[Expr.Arguments.IndexOf(x)];
                var mb = x.Member;
                if (x.Expression is MemberExpression)
                {

                    var mp = sql.MapFields.FirstOrDefault(p => p.Member == mb);
                    sql.MapFields.Add(new MapFieldInfo()
                    {
                        Alias = (mp != null) ? mp.Alias : sql.Alias,
                        AliasName = mb.Name,
                        Member = mb as PropertyInfo,
                        Name = ((MemberExpression)x.Expression).Member.Name,
                        Schema = (mp != null) ? mp.Schema : null,
                        TableName = (mp != null) ? mp.TableName : null
                    });
                }
                else
                {
                    sql.MapFields.Add(new MapFieldInfo()
                    {
                        AliasName = mb.Name,
                        Member = mb as PropertyInfo,
                        Name = mb.Name

                    });
                }

                var t = Compile(sql, x.Expression, x.Member);
                if (t.Op != null)
                {
                    var F = sql.MapFields.FirstOrDefault(p => p.Member == mb);
                    if (F != null)
                    {
                        F.ExprField = t.Clone();
                    }
                }
                if (t.Field != null)
                {
                    t.Field.AliasName = mb.Name;
                }
                ret.Add(t);
            }
            return ret;
        }

        public static List<TreeExpr> GetSelectedFieldsFromNewExpression<T>(Sql<T> sql, NewExpression Expr)
        {

            var ret = new List<TreeExpr>();
            foreach (var x in Expr.Arguments)
            {
                var mb = Expr.Members[Expr.Arguments.IndexOf(x)];
                if (x is MemberExpression)
                {

                    var mp = sql.MapFields.FirstOrDefault(p => p.Member == mb);
                    sql.MapFields.Add(new MapFieldInfo()
                    {
                        Alias = (mp != null) ? mp.Alias : sql.Alias,
                        AliasName = mb.Name,
                        Member = mb as PropertyInfo,
                        Name = ((MemberExpression)x).Member.Name,
                        Schema = (mp != null) ? mp.Schema : null,
                        TableName = (mp != null) ? mp.TableName : null
                    });
                }
                var t = Compile(sql, x, mb);
                if (t.Field != null)
                {
                    t.Field.AliasName = mb.Name;
                }
                ret.Add(t);
            }
            return ret;
        }
       
        public static TreeExpr CompileConst(BaseSql sql, ConstantExpression expr, MemberInfo mB)
        {
            var val = new ValueExpr
            {
                Type = expr.Type,
                Val = Expression.Lambda(expr).Compile().DynamicInvoke(),
                AliasName=(mB!=null)?mB.Name:null
            };
            
            var ret= new TreeExpr
            {
                Value = val
            };
           
            return ret;
        }
        public static ParameterExpression GetParamFromMember(MemberExpression expr)
        {
            var ret = expr.Expression;
            if (ret is ParameterExpression)
            {
                return ret as ParameterExpression;
            }
            else if (ret is MemberExpression)
            {
                return GetParamFromMember((MemberExpression)ret);
            }
            return null;
        }
        public static TreeExpr CompileMember(BaseSql sql, MemberExpression expr, MemberInfo mB)
        {
            ParameterExpression P = GetParamFromMember(expr);
            var mb = sql.MapFields.Where(p => p.ParamExpr != null).FirstOrDefault(p => p.ParamExpr == P && p.Member == expr.Member);
            if (mb == null)
            {
                mb = sql.MapFields.FirstOrDefault(p => p.Member == expr.Member);
            }
            if (mb == null)
            {
                mb = sql.MapFields.FirstOrDefault(p => p.Member.Name == expr.Member.Name);
            }
            if (mb != null)
            {
                if (mb.ExprField != null)
                {
                    return mb.ExprField;
                }
                return new TreeExpr
                {
                    Field = new FieldExpr
                    {
                        AliasName = (mB != null && mB.Name != expr.Member.Name) ? mB.Name : (mb.AliasName ?? null),
                        Name = mb.Name,
                        Schema = mb.Schema,
                        TableName = mb.TableName
                    }
                };
            }

            throw new NotImplementedException();
        }



        public static TreeExpr CompileBin(BaseSql sql, BinaryExpression expr, MemberInfo mb)
        {
            return new TreeExpr
            {
                AliasName = (mb != null) ? mb.Name : null,
                Op = GetOp(expr.NodeType),
                Left = Compile(sql, expr.Left, mb),
                Right = Compile(sql, expr.Right, mb)
            };
        }

        public static string GetOp(ExpressionType nodeType)
        {
            if (nodeType == ExpressionType.Equal)
            {
                return "=";
            }
            if (nodeType == ExpressionType.Add)
            {
                return "+";
            }
            if (nodeType == ExpressionType.NotEqual)
            {
                return "!=";
            }
            throw new NotImplementedException();
        }
        internal static bool IsPrimitiveType(Type declaringType)
        {
            if (declaringType.IsPrimitive)
            {
                return true;
            }
            else
            {
                if (declaringType == typeof(string))
                {
                    return true;
                }
                if (declaringType == typeof(DateTime))
                {
                    return true;
                }
                if (declaringType == typeof(object))
                {
                    return true;
                }
                if (declaringType == typeof(Decimal))
                {
                    return true;
                }
                else if (declaringType.IsClass)
                {
                    return false;
                }
            }
            return false;
        }
    }
}
