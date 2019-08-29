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
    internal class SQLServerTranslator : AbstractExpressionVisitor, ISQLTranslator
    {
        StringBuilder sb;
        SQLPredicate predicate;
        int Initial;
        string Alias = "x";

        //To check SQL rules
        bool HasOrderBy;
        bool HasOffset;
        bool IsNested;
        bool OffsetRequired;
        bool OrderByRequired;
        bool IgnoreAliases;

        public void ResetClauses()
        {
            HasOrderBy = false;
            HasOffset = false;
            OffsetRequired = false;
            OrderByRequired = false;
        }

        public SQLPredicate Translate(Expression expression, int initial = 0, bool ignoreAliases = false)
        {
            IgnoreAliases = ignoreAliases;
            IsNested = false;
            ResetClauses();
            Initial = initial;
            sb = new StringBuilder();
            predicate = new SQLPredicate();
            Visit(expression);
            predicate.Sql = sb.ToString();
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
            //checking requirements
            if (OffsetRequired && m.Method.DeclaringType == typeof(Queryable) && m.Method.Name != "Skip")
            {
                HasOffset = true; //This query has offset declared
                OrderByRequired = true; //This query requires an order by clause to be valid
                OffsetRequired = false;
                this.Visit(m);
                sb.Append(" OFFSET 0 ROWS ");
                return m;
            }

            if (OrderByRequired && m.Method.DeclaringType == typeof(Queryable) && (m.Method.Name != "OrderBy" & m.Method.Name != "OrderByDescending" & m.Method.Name != "ThenBy" & m.Method.Name != "ThenByDescending"))
            {
                OrderByRequired = false;
                this.Visit(m);
                sb.Append(" ORDER BY 1 "); //Ordering by the first query column
                return m;
            }

            //Exploring methods
            if (m.Method.DeclaringType == typeof(Queryable) && m.Method.Name == "Where")
            {
                LambdaExpression lambda = (LambdaExpression)StripQuotes(m.Arguments[1]);
                Alias = lambda.Parameters[0].Name;
                //adjusting query string by rules
                //nested ORDER BY requires TOP or OFFSET. If they aren't used, using TOP 10000000
                if (IsNested && HasOrderBy && !HasOffset) //adding top <big number> when order by exists without offset
                {
                    sb.Append("SELECT TOP 10000000 * FROM (");
                }
                else
                {
                    sb.Append("SELECT * FROM (");
                }
                IsNested = true; //nesting next statements inside where clause
                //reset clause booleans
                ResetClauses();
                this.Visit(m.Arguments[0]);
                sb.Append(") AS " + Alias + " WHERE ");
                this.Visit(lambda.Body);
                return m;
            }

            if (m.Method.DeclaringType == typeof(Queryable) && (m.Method.Name == "First" || m.Method.Name == "FirstOrDefault"))
            {
                IsNested = true; //nesting next statements inside this clause
                if (m.Arguments.Count == 1)
                {
                    sb.Append("SELECT TOP 1 * FROM (");
                    ResetClauses();
                    this.Visit(m.Arguments[0]);
                    sb.Append(") AS " + Alias);
                    return m;
                }
                else
                {
                    LambdaExpression lambda = (LambdaExpression)StripQuotes(m.Arguments[1]);
                    sb.Append("SELECT TOP 1 * FROM (");
                    Alias = lambda.Parameters[0].Name;
                    ResetClauses();
                    this.Visit(m.Arguments[0]);
                    sb.Append(") AS " + Alias + " WHERE ");
                    this.Visit(lambda.Body);
                    return m;
                }
            }

            if (m.Method.DeclaringType == typeof(Queryable) && m.Method.Name == "Count")
            {
                IsNested = true; //nesting next statements inside count clause
                if (m.Arguments.Count == 1)
                {
                    sb.Append("SELECT COUNT(*) FROM (");
                    ResetClauses();
                    this.Visit(m.Arguments[0]);
                    sb.Append(") AS " + Alias);
                    return m;
                }
                else
                {
                    LambdaExpression lambda = (LambdaExpression)StripQuotes(m.Arguments[1]);
                    sb.Append("SELECT COUNT(*) FROM (");
                    Alias = lambda.Parameters[0].Name;
                    ResetClauses();
                    this.Visit(m.Arguments[0]);
                    sb.Append(") AS " + Alias + " WHERE ");
                    this.Visit(lambda.Body);
                    return m;
                }
            }
            
            if (m.Method.DeclaringType == typeof(Queryable) && m.Method.Name == "Sum")
            {
                IsNested = true; //nesting next statements inside this clause
                if (m.Arguments.Count > 1)
                {
                    LambdaExpression lambda = (LambdaExpression)StripQuotes(m.Arguments[1]);
                    sb.Append("SELECT SUM(");
                    this.Visit(lambda.Body);
                    sb.Append(")");
                    sb.Append(" FROM (");
                    Alias = lambda.Parameters[0].Name;
                    this.Visit(m.Arguments[0]);
                    // if (IsSecondPredicate) sb.Append(") AS " + Alias);
                    sb.Append(") AS " + Alias);
                    return m;
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            if (m.Method.DeclaringType == typeof(Queryable) && m.Method.Name == "Select")
            {
                IsNested = true; //nesting next statements inside this clause
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
                    return m;
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            if (m.Method.DeclaringType == typeof(Queryable) && m.Method.Name == "Any")
            {
                IsNested = true; //nesting next statements inside count clause
                if (m.Arguments.Count == 1)
                {
                    sb.Append("SELECT CASE WHEN EXISTS(");
                    ResetClauses();
                    this.Visit(m.Arguments[0]);
                    sb.Append(") THEN 1 ELSE 0 END AS BIT");
                    return m;
                }
                else
                {
                    LambdaExpression lambda = (LambdaExpression)StripQuotes(m.Arguments[1]);
                    sb.Append("SELECT CASE WHEN EXISTS(");
                    sb.Append("SELECT * FROM(");
                    Alias = lambda.Parameters[0].Name;
                    ResetClauses();
                    this.Visit(m.Arguments[0]);
                    sb.Append(") AS " + Alias + " WHERE ");
                    this.Visit(lambda.Body);
                    sb.Append(") THEN 1 ELSE 0 END AS BIT");
                    return m;
                }
            }

            if (m.Method.DeclaringType == typeof(Queryable) && m.Method.Name == "Take")
            {
                OffsetRequired = true;
                this.Visit(m.Arguments[0]);
                sb.Append(" FETCH NEXT " + m.Arguments[1] + " ROWS ONLY ");
                return m;
            }

            if (m.Method.DeclaringType == typeof(Queryable) && m.Method.Name == "Skip")
            {
                HasOffset = true;
                OrderByRequired = true;
                OffsetRequired = false;
                this.Visit(m.Arguments[0]);
                sb.Append(" OFFSET " + m.Arguments[1] + " ROWS ");
                return m;
            }

            if (m.Method.DeclaringType == typeof(Queryable) && m.Method.Name == "OrderBy")
            {
                HasOrderBy = true;
                OrderByRequired = false;
                LambdaExpression lambda = (LambdaExpression)StripQuotes(m.Arguments[1]);
                Alias = lambda.Parameters[0].Name;
                this.Visit(m.Arguments[0]);
                sb.Append(" ORDER BY ");
                this.Visit(lambda.Body);
                return m;
            }

            if (m.Method.DeclaringType == typeof(Queryable) && m.Method.Name == "ThenBy")
            {
                HasOrderBy = true;
                OrderByRequired = false;LambdaExpression lambda = (LambdaExpression)StripQuotes(m.Arguments[1]);
                Alias = lambda.Parameters[0].Name;
                this.Visit(m.Arguments[0]);
                sb.Append(" ,");
                this.Visit(lambda.Body);
                return m;
            }

            if (m.Method.DeclaringType == typeof(Queryable) && m.Method.Name == "OrderByDescending")
            {
                HasOrderBy = true;
                OrderByRequired = false;
                LambdaExpression lambda = (LambdaExpression)StripQuotes(m.Arguments[1]);
                Alias = lambda.Parameters[0].Name;
                this.Visit(m.Arguments[0]);
                sb.Append(" ORDER BY ");
                this.Visit(lambda.Body);
                sb.Append(" DESC ");
                return m;
            }

            if (m.Method.DeclaringType == typeof(Queryable) && m.Method.Name == "ThenByDescending")
            {
                HasOrderBy = true;
                OrderByRequired = false;
                LambdaExpression lambda = (LambdaExpression)StripQuotes(m.Arguments[1]);
                Alias = lambda.Parameters[0].Name;
                this.Visit(m.Arguments[0]);
                sb.Append(" ,");
                this.Visit(lambda.Body);
                sb.Append(" DESC ");
                return m;
            }

            //Handling other methods
            return TranslateMethod(m, false);
            throw new NotSupportedException(string.Format("The method '{0}' is not supported", m.Method.Name));

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
                string fields = String.Join(", ", f.TableMapping.Values.Select(v => Alias + "." + "[" + v.ColumnName + "]"));
                string AppendToQuery = string.Empty;
                if (OffsetRequired)
                {
                    HasOffset = true;
                    OrderByRequired = true;
                    OffsetRequired = false;
                    AppendToQuery = " OFFSET 0 ROWS ";
                }
                if (OrderByRequired && !HasOrderBy)
                {
                    OrderByRequired = false;
                    HasOrderBy = true;
                    AppendToQuery = " ORDER BY 1 " + AppendToQuery;
                }
                if (IsNested && HasOrderBy && !HasOffset) //adding top <big number> when order by exists without offset
                {

                    sb.Append("SELECT TOP 10000000 ");
                }
                else
                {
                    sb.Append("SELECT ");
                }
                sb.Append(fields + " FROM " + f.FunctionName + "(" + String.Join(",", parameters) + ") AS " + Alias + AppendToQuery);
                Type type = f.ElementType;
            }
            else if (q != null)
            {
                // assume constant nodes w/ IQueryables are table references
                string fields = String.Join(", ", q.TableMapping.Values.Select(v => Alias + "." + "[" +  v.ColumnName + "]"));
                string AppendToQuery = string.Empty;
                if (OffsetRequired)
                {
                    HasOffset = true;
                    OrderByRequired = true;
                    OffsetRequired = false;
                    AppendToQuery = " OFFSET 0 ROWS ";
                }
                if (OrderByRequired && !HasOrderBy)
                {
                    OrderByRequired = false;
                    HasOrderBy = true;
                    AppendToQuery = " ORDER BY 1 " + AppendToQuery;
                }
                if (IsNested && HasOrderBy && !HasOffset) //adding top <big number> when order by exists without offset
                {
                    sb.Append("SELECT TOP 10000000 ");
                }
                else
                {
                    sb.Append("SELECT ");
                }
                sb.Append(fields + " FROM " + q.TableName + " AS " + Alias + AppendToQuery);
                Type type = q.ElementType;
            }
            else if (s != null)
            {
                sb.Append(c.ToString());
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
                        if (c.Value is DateTimeOffset)
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
            if (m.Expression != null && m.Expression.NodeType == ExpressionType.Parameter)
            {
                object entity = Expression.Lambda<Func<object>>(Expression.Convert(Expression.New(m.Expression.Type), typeof(object))).Compile()();
                var property = (PropertyInfo)m.Member;
                ColumnNameAttribute Column = entity.GetType().GetTypeInfo().GetProperty(property.Name).GetCustomAttribute<ColumnNameAttribute>();
                string ColumnName = Column == null ? entity.GetType().GetProperty(property.Name).Name : Column.ColumnName;
                if (IgnoreAliases)
                {
                    sb.Append("[" + ColumnName + "]");
                }
                else
                {
                    sb.Append(m.Expression + "." + "[" + ColumnName + "]");
                }
                return m;
            }
            if (m.Expression != null && (m.Expression.NodeType == ExpressionType.Constant || m.Expression.NodeType == ExpressionType.MemberAccess))
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