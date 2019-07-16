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
        public SQLPredicate SqlPredicate { get;  set; }
        string alias = "x";

        public QueryString(QueryProvider provider, SQLPredicate predicate)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            this.QueryProvider = provider;

            this.SqlPredicate = predicate;
            this.Predicate = Expression.Constant(this);
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

        public Dictionary<string, string> TableMapping { get; set; }

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
            return this.SqlPredicate.Sql;
        }
    }
}

