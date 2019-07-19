using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Dook.Attributes;

namespace Dook
{
    public abstract class DbFunction<T> : IMappedFunction<T>
    {
        protected QueryProvider QueryProvider;
        private Expression Predicate;
        string alias = "x";

        public DbFunction(QueryProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException("provider");
            }
            this.QueryProvider = provider;
            this.Predicate = Expression.Constant(this);
            GetTableData();
            GetParametersData();
        }

        public DbFunction(QueryProvider provider, Expression expression)
        {
            if (provider == null)
            {
                throw new ArgumentNullException("provider");
            }

            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }

            if (!typeof(IQueryable<T>).IsAssignableFrom(expression.Type))
            {
                throw new ArgumentOutOfRangeException("expression");
            }
            this.QueryProvider = provider;
            this.Predicate = expression;
            GetTableData();
            GetParametersData();
        }

        private void GetParametersData()
        {
            IndexedParameters = new Dictionary<int, string>();
            foreach (PropertyInfo p in this.GetType().GetProperties())
            {
                IsParameterAttribute ip = p.GetCustomAttribute<IsParameterAttribute>();
                if (ip != null)
                {
                    IndexedParameters.Add(ip.Index, p.Name);
                }
            }
        }

        private void GetTableData()
        {
            TableMapping = Mapper.GetTableMapping<T>();
            FunctionNameAttribute tableNameAtt = typeof(T).GetTypeInfo().GetCustomAttribute<FunctionNameAttribute>();
            FunctionName = tableNameAtt != null ? tableNameAtt.FunctionName : typeof(T).Name + "s";
            alias = FunctionName.First().ToString().ToLower();
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

        public string FunctionName { get; set; }

        public Dictionary<int, string> IndexedParameters { get; set; }

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
            return this.QueryProvider.GetQueryText(this.Predicate);
        }
    }
}

