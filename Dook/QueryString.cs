using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Dook.Attributes;

namespace Dook
{
    public class QueryString<T> : IMappedStringQueryable<T>
    {
        protected QueryProvider QueryProvider;
        private Expression Predicate;
        public SQLPredicate SQLPredicate { get; set; } = new SQLPredicate();
        string alias = "x";

        public QueryString(QueryProvider provider)
        {
            SqlQueryAttribute sqlQueryAttribute = typeof(T).GetTypeInfo().GetCustomAttribute<SqlQueryAttribute>();
            if (sqlQueryAttribute == null)
            {
                throw new Exception($"Generic type {nameof(T)} must define {nameof(SqlQueryAttribute)} to be used with {nameof(QueryString<T>)} class.");
            }
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            QueryProvider = provider;
            SQLPredicate.Sql = sqlQueryAttribute.Query;
            Predicate = Expression.Constant(this);
            if (typeof(T).GetInterfaces().Contains(typeof(IEntity))) GetTableData();
        }

        private void GetTableData()
        {
            TableMapping = Mapper.GetTableMapping<T>();
            TableNameAttribute tableNameAtt = typeof(T).GetTypeInfo().GetCustomAttribute<TableNameAttribute>();
            TableName = tableNameAtt != null ? tableNameAtt.TableName : typeof(T).Name + "s";
            alias = TableName.First().ToString().ToLower();
        }

        public void SetExpression(Expression exp)
        {
            this.Predicate = exp;
        }

        public Expression Expression
        {
            get { return this.Predicate; }
        }

        Type IQueryable.ElementType
        {
            get { return typeof(T); }
        }

        IQueryProvider IQueryable.Provider
        {
            get { return this.QueryProvider; }
        }

        public Dictionary<string, ColumnInfo> TableMapping { get; set; }

        public string TableName { get; set; }

        public string Alias
        {
            get { return alias; }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)this.QueryProvider.Execute(this.Predicate)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)this.QueryProvider.Execute(this.Predicate)).GetEnumerator();
        }

        public override string ToString()
        {
            return this.SQLPredicate.Sql;
        }
    }
}

