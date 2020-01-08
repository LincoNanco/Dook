using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Dook.Attributes;

[assembly: InternalsVisibleTo("Dook.Tests")] 
namespace Dook
{
    internal class MySQLTranslator : AbstractExpressionVisitor, ISQLTranslator
    {
        StringBuilder sb;
        SQLPredicate predicate;
        int Initial;
        string Alias = "x";
        bool HasOrderBy;
        bool HasWhere;
        bool HasGroupBy;
        bool IgnoreAliases;
        string ProcedureCall;
        Type prevCallerType;
        SortedDictionary<string, string> JoinCriteria = new SortedDictionary<string, string>();
        SortedDictionary<string, string> JoinType = new SortedDictionary<string, string>();
        Dictionary<string, string> Aliases = new Dictionary<string, string>();

        internal MySQLTranslator()
        {
        }

        public SQLPredicate Translate(Expression expression, int initial = 0, bool ignoreAliases = false)
        {
            IgnoreAliases = ignoreAliases;
            HasOrderBy = false;
            HasWhere = false;
            HasGroupBy = false;
            ProcedureCall = string.Empty;
            Initial = initial;
            sb = new StringBuilder();
            predicate = new SQLPredicate();
            Visit(expression);
            predicate.Sql = $"{ProcedureCall}{sb.ToString()}";
            //requiring included properties
            if (JoinCriteria.Count > 0)
            {
                List<string> joinPredicates = new List<string>();
                foreach(string type in JoinCriteria.Keys)
                {
                    joinPredicates.Add($"{JoinType[type]} {JoinCriteria[type]}");
                }
                predicate.Sql += String.Join(" ", joinPredicates);
            }
            return predicate;
        }

        private static Expression StripQuotes(Expression e)
        {
            while (e.NodeType == ExpressionType.Quote)
            {
                e = ((UnaryExpression)e).Operand;
            }
            return e;
        }

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            if ((m.Method.DeclaringType == typeof(Queryable) || m.Method.DeclaringType == typeof(Enumerable)) && m.Method.Name == "Where")
            {
                LambdaExpression lambda = (LambdaExpression)StripQuotes(m.Arguments[1]);
                Alias = lambda.Parameters[0].Name;
                if (m.Method.DeclaringType == typeof(Enumerable))
                {
                    Type childType = m.Arguments[0].Type.GenericTypeArguments[0];
                    MemberExpression memberExp = (MemberExpression) m.Arguments[0];
                    MemberInfo member = memberExp.Member;
                    string tableName = Mapper.GetTableName(childType);
                    Type declaringType = member.DeclaringType;
                    string joinPredicate = SetJoin(declaringType, childType, memberExp, false);
                    sb.Append($"SELECT * FROM {tableName} AS {Alias}{tableName} {joinPredicate} AND ");
                    this.Visit(lambda.Body);
                }
                else
                {
                    bool Nested = HasWhere;
                    if (Nested) sb.Append("SELECT * FROM (");
                    HasWhere = true;
                        this.Visit(m.Arguments[0]);
                    sb.Append(" WHERE ");
                    this.Visit(lambda.Body);
                    HasWhere = false;
                    if (Nested) sb.Append(") AS " + Alias);
                }
                return m;
            }

            if (m.Method.DeclaringType == typeof(Queryable) && (m.Method.Name == "First" || m.Method.Name == "FirstOrDefault"))
            {

                if (m.Arguments.Count == 1)
                {
                    // if (IsSecondPredicate) sb.Append("SELECT * FROM (");
                    this.Visit(m.Arguments[0]);
                    // if (IsSecondPredicate) sb.Append(") AS " + Alias);
                    sb.Append(" LIMIT 1 ");
                    // IsSecondPredicate = false;
                    return m;
                }
                else
                {
                    LambdaExpression lambda = (LambdaExpression)StripQuotes(m.Arguments[1]);
                    // if (IsSecondPredicate) sb.Append("SELECT * FROM (");
                    Alias = lambda.Parameters[0].Name;
                    this.Visit(m.Arguments[0]);
                    // if (IsSecondPredicate) sb.Append(") AS " + Alias);
                    sb.Append(" WHERE ");
                    this.Visit(lambda.Body);
                    sb.Append(" LIMIT 1 ");
                    return m;
                }
            }

            if ((m.Method.DeclaringType == typeof(Queryable) || m.Method.DeclaringType == typeof(Enumerable)) && m.Method.Name == "Count")
            {
                if (m.Arguments.Count == 1)
                {
                    sb.Append("SELECT COUNT(*) FROM (");
                    this.Visit(m.Arguments[0]);
                    sb.Append(") AS " + Alias);
                    // IsSecondPredicate = true;
                    return m;
                }
                else
                {
                    LambdaExpression lambda = (LambdaExpression)StripQuotes(m.Arguments[1]);
                    sb.Append("SELECT COUNT(*) FROM (");
                    Alias = lambda.Parameters[0].Name;
                    this.Visit(m.Arguments[0]);
                    sb.Append(") AS " + Alias);
                    sb.Append(" WHERE ");
                    this.Visit(lambda.Body);
                    // IsSecondPredicate = true;
                    return m;
                }
            }

            if (m.Method.DeclaringType == typeof(Queryable) && m.Method.Name == "Sum")
            {
                if (m.Arguments.Count > 1)
                {
                    LambdaExpression lambda = (LambdaExpression)StripQuotes(m.Arguments[1]);
                    sb.Append("SELECT SUM(");
                    this.Visit(lambda.Body);
                    sb.Append(")");
                    sb.Append(" FROM (");
                    // if (IsSecondPredicate) sb.Append("SELECT * FROM (");
                    Alias = lambda.Parameters[0].Name;
                    this.Visit(m.Arguments[0]);
                    // if (IsSecondPredicate) sb.Append(") AS " + Alias);
                    sb.Append(") AS " + Alias);
                    // IsSecondPredicate = true;
                    return m;
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            if (m.Method.DeclaringType == typeof(Queryable) && m.Method.Name == "Select")
            {
                if (m.Arguments.Count > 1)
                {
                    LambdaExpression lambda = (LambdaExpression)StripQuotes(m.Arguments[1]);
                    sb.Append("SELECT ");
                    if (lambda.Body.CanReduce)
                    {
                        BlockExpression e =  (BlockExpression) lambda.Body.Reduce();
                        for (int i = 1; i < e.Expressions.Count - 1; i++)
                        {
                            this.Visit(((BinaryExpression)e.Expressions[i]).Right);
                            if (i < e.Expressions.Count - 2) sb.Append(", ");
                        }
                    } 
                    else
                    {
                        this.Visit(lambda.Body);
                    }
                    sb.Append(" FROM (");
                    Alias = lambda.Parameters[0].Name;
                    this.Visit(m.Arguments[0]);
                    sb.Append(") AS " + Alias);
                    // IsSecondPredicate = true;
                    return m;
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            if (m.Method.DeclaringType == typeof(Queryable) && m.Method.Name == "Max")
            {
                if (m.Arguments.Count > 1)
                {
                    LambdaExpression lambda = (LambdaExpression)StripQuotes(m.Arguments[1]);
                    sb.Append("SELECT MAX(");
                    if (lambda.Body.CanReduce)
                    {
                        BlockExpression e =  (BlockExpression) lambda.Body.Reduce();
                        for (int i = 1; i < e.Expressions.Count - 1; i++)
                        {
                            this.Visit(((BinaryExpression)e.Expressions[i]).Right);
                            if (i < e.Expressions.Count - 2) sb.Append(", ");
                        }
                    } 
                    else
                    {
                        this.Visit(lambda.Body);
                    }
                    sb.Append(") FROM (");
                    Alias = lambda.Parameters[0].Name;
                    this.Visit(m.Arguments[0]);
                    sb.Append(") AS " + Alias);
                    // IsSecondPredicate = true;
                    return m;
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            if (m.Method.DeclaringType == typeof(Queryable) && m.Method.Name == "Min")
            {
                if (m.Arguments.Count > 1)
                {
                    LambdaExpression lambda = (LambdaExpression)StripQuotes(m.Arguments[1]);
                    sb.Append("SELECT MIN(");
                    if (lambda.Body.CanReduce)
                    {
                        BlockExpression e =  (BlockExpression) lambda.Body.Reduce();
                        for (int i = 1; i < e.Expressions.Count - 1; i++)
                        {
                            this.Visit(((BinaryExpression)e.Expressions[i]).Right);
                            if (i < e.Expressions.Count - 2) sb.Append(", ");
                        }
                    } 
                    else
                    {
                        this.Visit(lambda.Body);
                    }
                    sb.Append(") FROM (");
                    Alias = lambda.Parameters[0].Name;
                    this.Visit(m.Arguments[0]);
                    sb.Append(") AS " + Alias);
                    // IsSecondPredicate = true;
                    return m;
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            if (m.Method.DeclaringType == typeof(Queryable) && m.Method.Name == "Any")
            {
                if (m.Arguments.Count == 1)
                {
                    sb.Append("SELECT EXISTS(");
                    this.Visit(m.Arguments[0]);
                    sb.Append(")");
                    // IsSecondPredicate = true;
                    return m;
                }
                else
                {
                    sb.Append("SELECT EXISTS(");
                    // if (IsSecondPredicate) sb.Append("SELECT * FROM (");
                    LambdaExpression lambda = (LambdaExpression)StripQuotes(m.Arguments[1]);
                    Alias = lambda.Parameters[0].Name;
                    this.Visit(m.Arguments[0]);
                    // if (IsSecondPredicate) sb.Append(") AS " + Alias);
                    sb.Append(" WHERE ");
                    this.Visit(lambda.Body);
                    sb.Append(")");
                    // IsSecondPredicate = true;
                    return m;
                }
            }

            if (m.Method.DeclaringType == typeof(Queryable) && m.Method.Name == "Take")
            {
                this.Visit(m.Arguments[0]);
                sb.Append(" LIMIT " + m.Arguments[1]);
                return m;
            }

            if (m.Method.DeclaringType == typeof(Queryable) && m.Method.Name == "Skip")
            {
                this.Visit(m.Arguments[0]);
                sb.Append(" OFFSET " + m.Arguments[1]);
                return m;
            }

            if (m.Method.DeclaringType == typeof(Queryable) && m.Method.Name == "OrderBy")
            {
                LambdaExpression lambda = (LambdaExpression)StripQuotes(m.Arguments[1]);
                Alias = lambda.Parameters[0].Name;
                bool Nested = HasWhere || HasOrderBy || HasGroupBy;
                if (Nested) sb.Append("SELECT * FROM (");
                HasWhere = false;
                HasOrderBy = true;
                this.Visit(m.Arguments[0]);
                sb.Append(" ORDER BY ");
                this.Visit(lambda.Body);
                HasOrderBy = false;
                if (Nested) sb.Append(") AS " + Alias);
                return m;
            }

            if (m.Method.DeclaringType == typeof(Queryable) && m.Method.Name == "ThenBy")
            {
                LambdaExpression lambda = (LambdaExpression)StripQuotes(m.Arguments[1]);
                Alias = lambda.Parameters[0].Name;
                this.Visit(m.Arguments[0]);
                sb.Append(" ,");
                this.Visit(lambda.Body);
                return m;
            }

            if (m.Method.DeclaringType == typeof(Queryable) && m.Method.Name == "OrderByDescending")
            {
                LambdaExpression lambda = (LambdaExpression)StripQuotes(m.Arguments[1]);
                Alias = lambda.Parameters[0].Name;
                bool Nested = HasWhere || HasGroupBy || HasOrderBy;
                if (Nested) sb.Append("SELECT * FROM (");
                HasWhere = false;
                HasOrderBy = true;
                this.Visit(m.Arguments[0]);
                sb.Append(" ORDER BY ");
                this.Visit(lambda.Body);
                sb.Append(" DESC ");
                HasOrderBy = false;
                if (Nested) sb.Append(") AS " + Alias);
                return m;
            }

            if (m.Method.DeclaringType == typeof(Queryable) && m.Method.Name == "ThenByDescending")
            {
                LambdaExpression lambda = (LambdaExpression)StripQuotes(m.Arguments[1]);
                Alias = lambda.Parameters[0].Name;
                this.Visit(m.Arguments[0]);
                sb.Append(" ,");
                this.Visit(lambda.Body);
                sb.Append(" DESC ");
                return m;
            }
            if (m.Method.Name == "Include" || m.Method.Name == "ThenInclude")
            {
                LambdaExpression lambda = (LambdaExpression)StripQuotes(m.Arguments[1]);
                Type childType = lambda.ReturnType.GenericTypeArguments[0];
                Type type = m.Arguments[0].Type.GenericTypeArguments[0];
                MemberExpression member = (MemberExpression) lambda.Body;
                //adding join clauses to be included when the query is ready
                SetJoin(type, childType, member);
                this.Visit(m.Arguments[0]);
                return m;
            }

            return TranslateMethod(m, false);
            throw new NotSupportedException(string.Format("The method '{0}' is not supported", m.Method.Name));

        }

        /// <summary>
        /// Sets JOIN clauses when needed
        /// </summary>
        /// <param name="type"></param>
        /// <param name="childType"></param>
        /// <param name="member"></param>
        private string SetJoin(Type type, Type childType, MemberExpression memberExpression, bool included = true)
        {
                string joinPredicate = string.Empty;
                //skipping if child type already included
                bool addData = !predicate.Aliases.ContainsKey(childType.Name);
                string childTableName = Mapper.GetTableName(childType);
                string tableName = Mapper.GetTableName(type);
                string alias = ((ParameterExpression) memberExpression.Expression).Name;
                Dictionary<string, ColumnInfo> mapping = Mapper.GetTableMapping(type);
                Dictionary<string, ColumnInfo> childMapping = Mapper.GetTableMapping(childType);
                // predicate.Aliases.Add(type.Name, $"{Alias}");
                // predicate.TableMappings.Add(type.Name, mapping);
                if (included && addData) predicate.Aliases.Add(childType.Name, $"{Alias}{childTableName}");
                if (included && addData) predicate.TableMappings.Add(childType.Name, childMapping);
                //skipping if join is already set for child type
                addData &= !JoinCriteria.ContainsKey(childType.Name);
                InvertedPropertyAttribute ipa = memberExpression.Member.GetCustomAttribute<InvertedPropertyAttribute>();
                ForeignKeyAttribute fka = memberExpression.Member.GetCustomAttribute<ForeignKeyAttribute>();
                ManyToManyAttribute mtm = memberExpression.Member.GetCustomAttribute<ManyToManyAttribute>();
                if (ipa == null && fka == null && mtm == null) throw new Exception($"No relationship attribute (InvertedProperty, ForeignKey or ManyToMany) declared for property {memberExpression.Member.Name}. Cannot invoke Include method for this property.");
                if (ipa != null)
                {
                    string predicate = $"{childTableName} AS {Alias}{childTableName} ON {alias}.{mapping["Id"].ColumnName} = {Alias}{childTableName}.{childMapping[ipa.PropertyName].ColumnName} ";
                    if (included && addData) JoinCriteria.Add(childType.Name, predicate);
                    if (included && addData) JoinType.Add(childType.Name, " LEFT JOIN ");
                    joinPredicate = $"INNER JOIN {predicate}";
                    // sb.Append($" LEFT JOIN {childTableName} AS {Alias}{childTableName} ON {Alias}.{mapping["Id"].ColumnName} = {Alias}{childTableName}.{childMapping[ipa.PropertyName].ColumnName} ");
                }
                if (fka != null)
                {
                    string predicate = $"{childTableName} AS {Alias}{childTableName} ON {alias}.{mapping["Id"].ColumnName} = {Alias}{childTableName}.{childMapping[fka.ForeignKey].ColumnName} ";
                    if (included && addData) JoinCriteria.Add(childType.Name, predicate);
                    if (included && addData) JoinType.Add(childType.Name, " LEFT JOIN ");
                    joinPredicate = $"INNER JOIN {predicate}";
                    // sb.Append($" LEFT JOIN {childTableName} AS {Alias}{childTableName} ON {Alias}.{mapping["Id"].ColumnName} = {Alias}{childTableName}.{childMapping[fka.ForeignKey].ColumnName} ");
                }
                if (mtm != null)
                {
                    string intermediateTableName = Mapper.GetTableName(mtm.IntermediateType);
                    Type intermediateType = mtm.IntermediateType;
                    Dictionary<string, ColumnInfo> intermediateMapping = Mapper.GetTableMapping(mtm.IntermediateType);
                    // predicate.Aliases.Add(intermediateType.Name, $"{Alias}{intermediateTableName}");
                    // predicate.TableMappings.Add(intermediateType.Name, intermediateMapping);
                    string predicate1 = $"{intermediateTableName} AS {Alias}{intermediateTableName} ON {alias}.{mapping["Id"].ColumnName} = {Alias}{intermediateTableName}.{intermediateMapping[mtm.ForeignKey].ColumnName} ";
                    if (included && addData) JoinCriteria.Add(intermediateType.Name, predicate1);
                    if (included && addData) JoinType.Add(intermediateType.Name, " LEFT JOIN ");
                    string predicate2 = $"{childTableName} AS {Alias}{childTableName} ON {Alias}{intermediateTableName}.{intermediateMapping[mtm.TheOtherForeignKey].ColumnName} = {Alias}{childTableName}.{childMapping["Id"].ColumnName} ";
                    if (included && addData) JoinCriteria.Add(childType.Name, predicate2);
                    if (included && addData) JoinType.Add(childType.Name, " LEFT JOIN ");
                    joinPredicate = $" INNER JOIN {predicate1} INNER JOIN {predicate2}";
                    // sb.Append($" LEFT JOIN {intermediateTableName} AS {Alias}{intermediateTableName} ON {Alias}.{mapping["Id"].ColumnName} = {Alias}{intermediateTableName}.{intermediateMapping[mtm.ForeignKey].ColumnName} ");
                    // sb.Append($" LEFT JOIN {childTableName} AS {Alias}{childTableName} ON {Alias}{intermediateTableName}.{intermediateMapping[mtm.TheOtherForeignKey].ColumnName} = {Alias}{childTableName}.{childMapping["Id"].ColumnName} ");
                }
                return joinPredicate;
        }

        /// <summary>
        /// Magic trick.
        /// </summary>
        /// <returns>The volunteer.</returns>
        /// <param name="member">Shazam!! (Claps!).</param>
        private static object GetValue(Expression member)
        {
            // source: http://stackoverflow.com/a/2616980/291955
            var objectMember = Expression.Convert(member, typeof(object));
            var getterLambda = Expression.Lambda<Func<object>>(objectMember);
            var getter = getterLambda.Compile();
            return getter();
        }

        protected override Expression VisitUnary(UnaryExpression u)
        {
            switch (u.NodeType)
            {
                case ExpressionType.Not:
                    if (u.Operand is MethodCallExpression)
                    {
                        TranslateMethod((MethodCallExpression)u.Operand, true);
                        break;
                    }
                    sb.Append(" NOT ");
                    this.Visit(u.Operand);
                    break;
                case ExpressionType.Convert:
                    this.Visit(u.Operand);
                    break;
                default:
                    throw new NotSupportedException(string.Format("The unary operator '{0}' is not supported", u.NodeType));
            }
            return u;
        }

        protected override Expression VisitBinary(BinaryExpression b)
        {
            sb.Append("(");
            this.Visit(b.Left);
            if (b.Right is ConstantExpression && ((ConstantExpression)b.Right).Value == null)
            {
                sb.Append(" IS " + (b.NodeType == ExpressionType.NotEqual ? "NOT " : "") + " NULL) ");
                return b;
            }
            switch (b.NodeType)
            {
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    sb.Append(" AND ");
                    break;
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    sb.Append(" OR ");
                    break;
                case ExpressionType.Equal:
                    sb.Append(" = ");
                    break;
                case ExpressionType.NotEqual:
                    sb.Append(" <> ");
                    break;
                case ExpressionType.LessThan:
                    sb.Append(" < ");
                    break;
                case ExpressionType.LessThanOrEqual:
                    sb.Append(" <= ");
                    break;
                case ExpressionType.GreaterThan:
                    sb.Append(" > ");
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    sb.Append(" >= ");
                    break;
                default:
                    throw new NotSupportedException(string.Format("The binary operator '{0}' is not supported", b.NodeType));
            }
            this.Visit(b.Right);
            sb.Append(")");
            return b;
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            IMappedFunction f = c.Value as IMappedFunction;
            IMappedQueryable q = c.Value as IMappedQueryable;
            IMappedStringQueryable s = c.Value as IMappedStringQueryable;
            if (f != null)
            {
                // assume constant nodes w/ IMappedFunction are table valued function references
                List<string> parameters = new List<string>();
                int fpCount = 0;
                foreach (int pKey in f.IndexedParameters.Keys.OrderBy(k => k))
                {
                    string name = "@FP" + fpCount;
                    parameters.Add(name);
                    predicate.Parameters.Add(name, f.GetType().GetProperty(f.IndexedParameters[pKey]).GetValue(f));
                    fpCount++;
                }
                string fields = String.Join(", ", f.TableMapping.Values.Select(v => Alias + "." + v.ColumnName));
                ProcedureCall = $"CALL {f.FunctionName} ({String.Join(",", parameters)}); ";
                sb.Append($"SELECT {fields} FROM Temp{f.FunctionName} AS {Alias}");
                Type type = f.ElementType;
            }
            else if (q != null)
            {
                // assume constant nodes w/ IMappedQueryable are table references
                string fields = String.Join(", ", q.TableMapping.Values.Select(v => Alias + "." + v.ColumnName));
                if (predicate.TableMappings.Any()) fields += ", " + String.Join(", ", predicate.TableMappings.Select(kvp => String.Join(", ", kvp.Value.Values.Select(v => predicate.Aliases[kvp.Key] + "." + v.ColumnName))));
                sb.Append($"SELECT {fields} FROM {q.TableName} AS {Alias}");
                Type type = q.ElementType;
            }
            else if (s != null)
            {
                sb.Append($"SELECT * FROM ({c.ToString()}) AS {Alias}");
            }
            else if (c.Value == null)
            {
                sb.Append("NULL");
            }
            else
            {
                string parameterName = "@P" + (predicate.Parameters.Count + Initial);
                switch (Type.GetTypeCode(c.Value.GetType()))
                {
                    case TypeCode.Object:
                        if (c.Value is TimeSpan)
                        {
                            sb.Append(parameterName);
                            predicate.Parameters.Add(parameterName, c.Value);
                            break;
                        }
                        throw new NotSupportedException(string.Format("The constant for '{0}' is not supported. ", c.Value));
                    default:
                        sb.Append(parameterName);
                        predicate.Parameters.Add(parameterName, c.Value);
                        break;
                }
            }
            return c;
        }

        protected override Expression VisitMemberAccess(MemberExpression m)
        {
            
            if (m.Expression != null && (m.Expression.NodeType == ExpressionType.Parameter || m.Expression.Type.IsAssignableFrom(typeof(IEntity))))
            {
                object entity = Expression.Lambda<Func<object>>(Expression.Convert(Expression.New(m.Expression.Type), typeof(object))).Compile()();
                var property = (PropertyInfo)m.Member;
                ColumnNameAttribute Column = entity.GetType().GetTypeInfo().GetProperty(property.Name).GetCustomAttribute<ColumnNameAttribute>();
                string ColumnName = Column == null ? entity.GetType().GetProperty(property.Name).Name : Column.ColumnName;
                if(IgnoreAliases)
                {
                    sb.Append(ColumnName);
                }
                else
                {
                    sb.Append(m.Expression + "." + ColumnName);
                }
                return m;
            }
            else if (m.Expression != null && (m.Expression.NodeType == ExpressionType.Constant || m.Expression.NodeType == ExpressionType.MemberAccess))
            {
                var f = Expression.Lambda(m).Compile();
                var value = f.DynamicInvoke();
                string parameterName = "@P" + (predicate.Parameters.Count + Initial);
                sb.Append(parameterName);
                predicate.Parameters.Add(parameterName, value);
                return m;
            }
            throw new NotSupportedException(string.Format("The member '{0}' is not supported", m.Member.Name));
        }

        protected Expression TranslateMethod(MethodCallExpression m, bool isNegated)
        {
            //Non translated methods
            if (m.Method == typeof(string).GetMethod("Contains", new[] { typeof(string) }))
            {
                Visit(m.Object);
                if (isNegated) sb.Append(" NOT");
                sb.Append(" LIKE ");
                sb.Append("@P" + (predicate.Parameters.Count + Initial));
                predicate.Parameters.Add("@P" + (predicate.Parameters.Count + Initial), "%" + ((ConstantExpression)m.Arguments[0]).Value + "%");
                return m;
            }
            if (m.Method == typeof(string).GetMethod("StartsWith", new[] { typeof(string) }))
            {
                Visit(m.Object);
                if (isNegated) sb.Append(" NOT");
                sb.Append(" LIKE ");
                sb.Append("@P" + (predicate.Parameters.Count + Initial));
                predicate.Parameters.Add("@P" + (predicate.Parameters.Count + Initial), ((ConstantExpression)m.Arguments[0]).Value + "%");
                return m;

            }
            if (m.Method == typeof(string).GetMethod("EndsWith", new[] { typeof(string) }))
            {
                Visit(m.Object);
                if (isNegated) sb.Append(" NOT");
                sb.Append(" LIKE ");
                sb.Append("@P" + (predicate.Parameters.Count + Initial));
                predicate.Parameters.Add("@P" + (predicate.Parameters.Count + Initial), "%" + ((ConstantExpression)m.Arguments[0]).Value);
                return m;
            }

            // IN queries:
            if (m.Method.Name == "Contains")
            {
                Expression collection;
                Expression property;
                if (m.Method.IsDefined(typeof(ExtensionAttribute)) && m.Arguments.Count == 2)
                {
                    collection = m.Arguments[0];
                    property = m.Arguments[1];
                }
                else if (!m.Method.IsDefined(typeof(ExtensionAttribute)) && m.Arguments.Count == 1)
                {
                    collection = m.Object;
                    property = m.Arguments[0];
                }
                else
                {
                    throw new Exception("Unsupported method call: " + m.Method.Name);
                }
                Visit(property);
                var values = (IEnumerable)GetValue(collection);
                List<object> list = new List<object>();
                foreach (var item in values)
                {
                    string parameterName = "@P" + (predicate.Parameters.Count + Initial);
                    predicate.Parameters.Add(parameterName, item);
                    list.Add(parameterName);
                }
                if (isNegated) sb.Append(" NOT");
                sb.Append(" IN (");
                if (list.Count > 0)
                {
                    sb.Append(String.Join(",", list));
                }
                else
                {
                    sb.Append("NULL");
                }
                sb.Append(") ");
                return m;
            }
            throw new NotSupportedException(string.Format("The method '{0}' is not supported", m.Method.Name));
        }
    }
}