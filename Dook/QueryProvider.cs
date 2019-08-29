using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Dook
{
    /// <summary>
    /// QueryProvider receives LINQ Expressions and returns SQL query strings.
    /// </summary>
    public class QueryProvider : IQueryProvider
    {
        private DbProvider DbProvider;

        public string ConnectionString { get {return DbProvider.ConnectionString; } }
        public DbType DbType { get { return DbProvider.DbType; } }
        public IDbConnection Connection { get { return DbProvider.Connection;  } }

        public QueryProvider(DbProvider DbProvider)
        {
            this.DbProvider = DbProvider;
        }

        /// <summary>
        /// Uses QueryTranslator to translate a LINQ Expression to a SQL query.
        /// </summary>
        /// <returns>The sql query.</returns>
        /// <param name="expression">The LINQ Expression to be translated.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        private SQLPredicate Translate(Expression expression, bool ignoreAliases = false)
        {   
            ISQLTranslator sql = SQLTranslatorFactory.GetTranslator(DbType);
            SQLPredicate pred = sql.Translate(Evaluator.PartialEval(expression), 0, ignoreAliases);
            return pred;
        }

        /// <summary>
        /// Gets an SQL query translated from a LINQ Expression
        /// </summary>
        /// <returns>The query text.</returns>
        /// <param name="expression">Expression.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public string GetQueryText(Expression expression)
        {
            return Translate(expression).Sql;
        }

        public IDbCommand GetNewCommand()
        {
            return DbProvider.GetCommand();
        }

        public IDbCommand GetUpdateCommand<T>(T entity, string TableName, Dictionary<string, ColumnInfo> TableMapping) where T : IEntity, new()
        {
            if (entity.Id == 0) throw new Exception("Id property must be a positive integer.");
            StringBuilder query = new StringBuilder();
            if (entity is ITrackDateOfChange)
            {
                ((ITrackDateOfChange)entity).UpdatedOn = DateTime.Now;
            }
            query.Append("UPDATE ");
            query.Append(TableName);
            query.Append(" SET ");
            //Building update string                   
            bool starting = true;
            string us = string.Empty;
            foreach (string p in TableMapping.Keys)
            {
                if (p != "Id" && p != "CreatedOn")
                {
                    if (starting)
                    {
                        starting = false;
                    }
                    else
                    {
                        us += ", ";
                    }
                    us += TableMapping[p].ColumnName + " = @" + p;
                }
            }
            query.Append(us);
            query.Append(" WHERE " + TableMapping["Id"].ColumnName + " = @id;");
            IDbCommand cmd = DbProvider.GetCommand();
            cmd.CommandText = query.ToString();
            //MySqlCommand cmd = new MySqlCommand(query.ToString());
            Type Type = typeof(T);
            foreach (string p in TableMapping.Keys)
            {
                if (p != "Id" && p != "CreatedOn")
                {
                    SetParameter(cmd, "@" + p, Type.GetProperty(p).GetValue(entity) ?? DBNull.Value);
                }
            }
            SetParameter(cmd, "@id", entity.Id);
            return cmd;
        }

        public IDbCommand GetInsertCommand<T>(T entity, string TableName, Dictionary<string, ColumnInfo> TableMapping) where T : IEntity, new()
        {
            StringBuilder query = new StringBuilder();
            if (entity is ITrackDateOfCreation)
            {
                ((ITrackDateOfCreation) entity).CreatedOn = DateTime.Now;
            }
            if (entity is ITrackDateOfChange)
            {
                ((ITrackDateOfChange)entity).UpdatedOn = DateTime.Now;
            }
            query.Append("INSERT INTO ");
            query.Append(TableName);
            //Building field and values string
            string fields = string.Empty;
            string values = string.Empty;
            bool starting = true;
            Type Type = entity.GetType();
            foreach (string p in TableMapping.Keys)
            {
                //I call entity.GetType so I can insert into database classes that inherit from class T.
                //TODO: I need to make sure that all this code behaves this way.
                if (p != "Id" && Type.GetProperty(p).GetValue(entity) != null)
                {
                    if (starting)
                    {
                        fields += " (";
                        values += "(";
                        starting = false;
                    }
                    else
                    {
                        fields += ", ";
                        values += ", ";
                    }
                    fields += TableMapping[p].ColumnName;
                    values += "@" + p;
                }
            }
            fields += ")";
            values += ")";
            query.Append(fields);
            query.Append(" VALUES ");
            query.Append(values);
            query.Append("; SELECT @@IDENTITY;");
            IDbCommand cmd = DbProvider.GetCommand();
            cmd.CommandText = query.ToString();
            foreach (string p in TableMapping.Keys)
            {
                if (p != "Id")
                {
                    SetParameter(cmd, "@" + p, Type.GetProperty(p).GetValue(entity) ?? DBNull.Value);
                }
            }
            return cmd;
        }

        public IDbCommand GetDeleteCommand(int id, string TableName, Dictionary<string, ColumnInfo> TableMapping)
        {
            string queryText = $"DELETE FROM {TableName} WHERE {TableMapping["Id"].ColumnName} = @id;";
            IDbCommand cmd = DbProvider.GetCommand();
            cmd.CommandText = queryText;
            SetParameter(cmd, "@id", id);
            return cmd;
        }

        public IDbCommand GetDeleteWhereCommand<T>(Expression<Func<T,bool>> expression, string TableName)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression), "The provided expression cannot be null.");
            SQLPredicate predicate = Translate(Evaluator.PartialEval(expression), true);
            LambdaExpression lambda = (LambdaExpression) expression;//for getting alias
            StringBuilder query = new StringBuilder();
            query.Append($"DELETE FROM {TableName} WHERE {predicate.Sql};");
            IDbCommand cmd = DbProvider.GetCommand();
            cmd.CommandText = query.ToString();
            predicate.SetParameters(cmd);
            return cmd;
        }

        public IDbCommand GetDeleteAllCommand(string TableName)
        {
            string queryText = $"DELETE FROM {TableName};";
            IDbCommand cmd = DbProvider.GetCommand();
            cmd.CommandText = queryText;
            return cmd;
        }

        public void SetParameter(IDbCommand cmd, string parameterName, object parameterValue)
        {
            var par = cmd.CreateParameter();
            par.ParameterName = parameterName;
            par.Value = parameterValue;
            cmd.Parameters.Add(par);
        }


        //IQueryProvider implementation
        public IQueryable CreateQuery(Expression expression)
        {
            Type elementType = TypeSystem.GetElementType(expression.Type);
            try
            {
                return (IQueryable)Activator.CreateInstance(typeof(Query<>).MakeGenericType(elementType), new object[] { this, expression });
            }
            catch (TargetInvocationException tie)
            {
                throw tie.InnerException;
            }
        }

        public IQueryable<T> CreateQuery<T>(Expression expression)
        {
            return new Query<T>(this, expression);
        }

        public object Execute(Expression expression)
        {
            IDbCommand cmd = DbProvider.GetCommand();
            SQLPredicate sql = Translate(expression);
            cmd.CommandText = sql.Sql;
            sql.SetParameters(cmd);
            cmd.Connection = DbProvider.Connection;
            try
            {
                IDataReader reader = cmd.ExecuteReader();
                Type elementType = TypeSystem.GetElementType(expression.Type);
                //TODO: for select to work properly, we need to identify whether the elementType is an IEnumerable or not
                if (!typeof(IQueryable).IsAssignableFrom(expression.Type))
                {
                    //This is to handle Count method
                    if (elementType == typeof(int))
                    {
                        reader.Read();
                        int result = Convert.ToInt32(reader[0] == DBNull.Value ? 0 : reader[0]);
                        reader.Dispose();
                        return result;
                    }
                    //Sum method
                    if (elementType == typeof(double))
                    {
                        reader.Read();
                        double result = Convert.ToDouble(reader[0] == DBNull.Value ? 0 : reader[0]);
                        reader.Dispose();
                        return result;
                    }
                    //Any method
                    if (elementType == typeof(bool))
                    {
                        reader.Read();
                        bool result = Convert.ToBoolean(reader[0]);
                        reader.Dispose();
                        return result;
                    }
                }
                if (elementType.IsPrimitive || elementType == typeof(string))
                {
                    return Activator.CreateInstance(
                    typeof(VariableReader<>).MakeGenericType(elementType),
                    BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new object[] { reader },
                    null);
                }
                return Activator.CreateInstance(
                typeof(ObjectReader<>).MakeGenericType(elementType),
                BindingFlags.Instance | BindingFlags.NonPublic, null,
                new object[] { reader },
                null);
                //}
            }
            catch (Exception e)
            {
                //TODO: Delete this in oproduction
                throw new Exception(e.Message + "." + " Query: " + cmd.CommandText);
            }
            //}
        }

        public T Execute<T>(Expression expression)
        {
            //This is to handle Count method
            if (typeof(T) == typeof(int))
            {
                return (T)this.Execute(expression);
            }
            if (typeof(T) == typeof(double))
            {
                return (T)this.Execute(expression);
            }
            if (typeof(T) == typeof(bool))
            {
                return (T)this.Execute(expression);
            }
            return ((IEnumerable<T>)this.Execute(expression)).FirstOrDefault();
        }
    }

    internal static class TypeSystem
    {
        internal static Type GetElementType(Type seqType)
        {
            Type ienum = FindIEnumerable(seqType);
            if (ienum == null) return seqType;
            return ienum.GetGenericArguments()[0];
        }

        private static Type FindIEnumerable(Type seqType)
        {
            if (seqType == null || seqType == typeof(string))
                return null;
            if (seqType.IsArray)
                return typeof(IEnumerable<>).MakeGenericType(seqType.GetElementType());
            if (seqType.IsGenericType)
            {
                foreach (Type arg in seqType.GetGenericArguments())
                {
                    Type ienum = typeof(IEnumerable<>).MakeGenericType(arg);
                    if (ienum.IsAssignableFrom(seqType))
                    {
                        return ienum;
                    }
                }
            }
            Type[] ifaces = seqType.GetInterfaces();
            if (ifaces != null && ifaces.Length > 0)
            {
                foreach (Type iface in ifaces)
                {
                    Type ienum = FindIEnumerable(iface);
                    if (ienum != null) return ienum;
                }
            }

            if (seqType.BaseType != null && seqType.BaseType != typeof(object))
            {
                return FindIEnumerable(seqType.BaseType);
            }
            return null;
        }
    }
}