using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Dook.Attributes;

namespace Dook
{
    /// <summary>
    /// QueryProvider receives LINQ Expressions and returns SQL query strings.
    /// </summary>
    public class JoinProvider
    {
        ISQLTranslator QueryTranslator;
        Dictionary<string, List<SQLPredicate>> QueryDictionary = new Dictionary<string, List<SQLPredicate>>(); //To store query filter generated for each Alias (and class)
        public Dictionary<string, string> AliasDictionary = new Dictionary<string, string>(); //To know what table is representen by which Alias 
        public Dictionary<string, Type> TypeDictionary = new Dictionary<string, Type>(); //To know which class is represented by each alias
        public Dictionary<string, int> IndexDictionary = new Dictionary<string, int>(); //To know the index where query results start for a particular Repo/Entity
        public List<string> Parameters = new List<string>();
        public Dictionary<string, Type> RepositoryDictionary = new Dictionary<string, Type>(); //To know which repository is storing query results for which alias.
        public Dictionary<string, Dictionary<string,ColumnInfo>> TableMappingDictionary = new Dictionary<string, Dictionary<string, ColumnInfo>>(); //To know which repository is storing query results for which alias.
        Dictionary<string, int> AddingOrder = new Dictionary<string, int>();
        Dictionary<string, JoinType> JoinTypeDictionary = new Dictionary<string, JoinType>();
        List<SQLPredicate> JoinFilters = new List<SQLPredicate>();
        DbProvider DbProvider;
        string Where = string.Empty;
        
        int i = 0;
        int lastPosition = 0;

        public JoinProvider(DbProvider provider)
        {
            DbProvider = provider;
            QueryTranslator = SQLTranslatorFactory.GetTranslator(provider.DbType);
        }

        public void SetParameter(IDbCommand cmd, string parameterName, object parameterValue)
        {
            var par = cmd.CreateParameter();
            par.ParameterName = parameterName;
            par.Value = parameterValue;
            cmd.Parameters.Add(par);
        }

        int GetFieldsCount(IEntity e)
        {
            TypeInfo t = e.GetType().GetTypeInfo();
            int NumberOfAttributes = t.GetProperties().Count(p => p.GetCustomAttribute<NotMappedAttribute>() == null);
            return NumberOfAttributes;
        }

        string GetTableName(IEntity e)
        {
            Type t = e.GetType();
            TableNameAttribute tableNameAtt = t.GetTypeInfo().GetCustomAttribute<TableNameAttribute>();
            return tableNameAtt != null ? tableNameAtt.TableName : t.Name + "s";
        }

        /// <summary>
        /// Gets a JOIN QueryPredicate based on a LINQ Expression with two parameters.
        /// </summary>
        /// <returns>The JOIN QueryPredicate.</returns>
        /// <param name="expression">The expression to be translated.</param>
        /// <typeparam name="T1">Left entity involved in the JOIN (it must inherit from IEntity class).</typeparam>
        /// <typeparam name="T2">Right entity involved in the JOIN (it must inherit from IEntity class).</typeparam>
        public void Join<T1, T2>(JoinType joinType, Expression<Func<T1, T2, bool>> expression, EntitySet<T1> entitySet1, EntitySet<T2> entitySet2) where T1 : class, IEntity, new() where T2 : class, IEntity, new()
        {

            T1 entity1 = new T1();
            T2 entity2 = new T2();
            string parameter1 = expression.Parameters[0].Name;
            string parameter2 = expression.Parameters[1].Name;

            if (joinType != JoinType.Inner && Parameters.Contains(parameter2)) throw new Exception($"Alias '" + parameter2 + " already used within a " + GetJoinType(JoinTypeDictionary[parameter2]) + "' operator. No further use with RIGHT JOIN or LEFT JOIN is allowed.");

			SQLPredicate queryPredicate = QueryTranslator.Translate(Evaluator.PartialEval(expression), i);
            i += queryPredicate.Parameters.Count;
            AddParameter(parameter1, entity1, typeof(EntitySet<T1>), typeof(T1), entitySet1, joinType);
            AddParameter(parameter2, entity2, typeof(EntitySet<T2>), typeof(T2), entitySet2, joinType);
            if (AddingOrder[parameter1] > AddingOrder[parameter2])
            {
                QueryDictionary[parameter1].Add(queryPredicate);
            }
            else
            {
                QueryDictionary[parameter2].Add(queryPredicate);
            }
        }

        public void AddJoinFilter<T>(Expression<Func<T, bool>> expression) where T : IEntity, new()
        {
            SQLPredicate predicate = QueryTranslator.Translate(Evaluator.PartialEval(expression),i);
            i += predicate.Parameters.Count;
            Where += (Where.Length > 0 ? " AND " : " WHERE ") + predicate.Sql;
            JoinFilters.Add(predicate);
        }

        void AddParameter<T>(string name, IEntity entity, Type repoType, Type type, EntitySet<T> entitySet, JoinType joinType = JoinType.Inner) where T : class, IEntity, new()
        {
            if (!Parameters.Contains(name)) {               
				Parameters.Add(name);
				AddingOrder.Add(name, Parameters.Count - 1);
				QueryDictionary.Add(name, new List<SQLPredicate>());
				AliasDictionary.Add(name, GetTableName(entity));
                RepositoryDictionary.Add(name, repoType);
                TableMappingDictionary.Add(name, entitySet.TableMapping);
                IndexDictionary.Add(name, lastPosition);
                lastPosition += GetFieldsCount(entity);
                TypeDictionary.Add(name, type);
                JoinTypeDictionary.Add(name, joinType);
            }
        }

        /// <summary>
        /// Builds the JOIN command to be executed.
        /// </summary>
        /// <returns>The INNER/RIGHT/LEFT JOIN command to be executed.</returns>
        public IDbCommand  GetJoinCommand()
        {
            IDbCommand JoinQuery = DbProvider.GetCommand();
            JoinQuery.CommandText = "SELECT " + GetCommandPredicate(JoinQuery);
            lastPosition = 0; //Reset last joined entity position to 0
            return JoinQuery;
        }

        private string GetCommandPredicate(IDbCommand command = null)
        {
            List<string> FieldStrings = new List<string>();
            string JoinQueryString = string.Empty;
            bool Start = true;
            foreach (string p in Parameters)
            {
                IEntity entity = (IEntity)Activator.CreateInstance(TypeDictionary[p]);
                foreach (string propertyName in TableMappingDictionary[p].Values.Select(x => x.ColumnName))
                {
                    FieldStrings.Add(p + "." + propertyName + " AS " + p + propertyName);
                }
                if (Start)
                {
                    JoinQueryString += AliasDictionary[p] + " AS " + p;
                    Start = false;
                }
                else
                {                  
					JoinQueryString += GetJoinType(JoinTypeDictionary[p]) + AliasDictionary[p] + " AS " + p;
                    for (int j = 0; j < QueryDictionary[p].Count; j++)
                    {
                        JoinQueryString += j == 0 ? " ON " + QueryDictionary[p][j].Sql : " AND " + QueryDictionary[p][j].Sql;
                        QueryDictionary[p][j].SetParameters(command);
                    }
                }
            }
            //string Where = string.Empty;
            foreach (SQLPredicate q in JoinFilters)
            {
                //Where += (Where.Length > 0 ? " AND " : " WHERE ") + q.Sql;
                q.SetParameters(command);
            }
            return  String.Join(",", FieldStrings) + " FROM " + JoinQueryString + Where;
        }

        string GetJoinType(JoinType joinType)
        {
            switch (joinType)
            {
                case JoinType.Inner:
                    return " JOIN ";
                case JoinType.Left:
                    return " LEFT JOIN ";
                case JoinType.Right:
                    return " RIGHT JOIN ";
                default:
                    return string.Empty;
            }
        }
    }

    public enum JoinType
    {
        Inner, Left, Right
    }
}