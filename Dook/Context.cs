using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace Dook
{
    public class Context : IDisposable
    {
        internal string ConnectionString;
        JoinProvider JoinProvider;
        protected QueryProvider QueryProvider;
        internal DbType DbType;
        protected DbProvider DbProvider;
        internal string Suffix;

        public Context(IDookConfigurationOptions configuration)
        {
            DbType = configuration.DatabaseType;
            ConnectionString = configuration.ConnectionString;
            DbProvider = new DbProvider(DbType, ConnectionString, configuration.CommandTimeout);
            JoinProvider = new JoinProvider(DbProvider);
            QueryProvider = new QueryProvider(DbProvider);
            DbProvider.Connection.Open();
            DbProvider.Transaction = DbProvider.Connection.BeginTransaction();
            Suffix = configuration.Suffix;
        }

        public IDbConnection GetConnection()
        {
            return DbProvider.Connection;
        }

        public void Join<T1, T2>(Expression<Func<T1,T2,bool>> expression, EntitySet<T1> T1Repository = null, EntitySet<T2> T2Repository = null) where T1 : class, IEntity, new() where T2 : class, IEntity, new()
        {
            EntitySet<T1> Repository1 = (EntitySet<T1>) GetType().GetProperty(typeof(T1).Name + Suffix).GetValue(this);
            EntitySet<T2> Repository2 = (EntitySet<T2>) GetType().GetProperty(typeof(T2).Name + Suffix).GetValue(this);
            JoinProvider.Join(JoinType.Inner, expression, Repository1, Repository2);
        }

        public void RightJoin<T1, T2>(Expression<Func<T1, T2, bool>> expression, EntitySet<T1> T1Repository = null, EntitySet<T2> T2Repository = null) where T1 : class, IEntity, new() where T2 : class, IEntity, new()
        {
            EntitySet<T1> Repository1 = (EntitySet<T1>)GetType().GetProperty(typeof(T1).Name + Suffix).GetValue(this);
            EntitySet<T2> Repository2 = (EntitySet<T2>)GetType().GetProperty(typeof(T2).Name + Suffix).GetValue(this);
            JoinProvider.Join(JoinType.Right, expression, Repository1, Repository2);
        }

        public void LeftJoin<T1, T2>(Expression<Func<T1, T2, bool>> expression, EntitySet<T1> T1Repository = null, EntitySet<T2> T2Repository = null) where T1 : class, IEntity, new() where T2 : class, IEntity, new()
        {
            EntitySet<T1> Repository1 = (EntitySet<T1>)GetType().GetProperty(typeof(T1).Name + Suffix).GetValue(this);
            EntitySet<T2> Repository2 = (EntitySet<T2>)GetType().GetProperty(typeof(T2).Name + Suffix).GetValue(this);
            JoinProvider.Join(JoinType.Left, expression, Repository1, Repository2);
        }

        public void AddFilter<T>(Expression<Func<T,bool>> expression = null) where T : IEntity, new()
        {
            if (expression != null)
            {
                JoinProvider.AddJoinFilter(expression);
            }
        }

        public void ExecuteJoin()
        {
            try
            {
                IDbCommand cmd = JoinProvider.GetJoinCommand();
                Dictionary<string, MethodInfo> MethodDictionary = new Dictionary<string, MethodInfo>();
                foreach (string alias in JoinProvider.RepositoryDictionary.Keys)
                {
                    MethodInfo m = JoinProvider.RepositoryDictionary[alias].GetMethod("AddFromReader");
                    MethodDictionary.Add(alias,m);
                    object Repository = GetType().GetProperty(JoinProvider.TypeDictionary[alias].Name + Suffix).GetValue(this);
                    object DataStore = Repository.GetType().GetField("JoinResults", BindingFlags.Public | BindingFlags.Instance).GetValue(Repository);
                    MethodInfo Clear = DataStore.GetType().GetMethod("Clear");
                    Clear.Invoke(DataStore, new object[]{});
                }
                using (IDataReader oReader = cmd.ExecuteReader())
                {
                    while (oReader.Read())
                    {
                        foreach (string alias in MethodDictionary.Keys)
                        {
                            object Repository = GetType().GetProperty(JoinProvider.TypeDictionary[alias].Name + Suffix).GetValue(this);
                            MethodDictionary[alias].Invoke(Repository, new object[]{ oReader, JoinProvider.IndexDictionary[alias] });
                        }
                    }
                }
                JoinProvider = new JoinProvider(DbProvider);
            }
            catch (Exception e)
            {
                JoinProvider = new JoinProvider(DbProvider);
                throw e;
            }
        }

        public void SaveChanges()
        {
            try
            {
                DbProvider.Transaction.Commit();
            }
            catch
            {
                DbProvider.Transaction.Rollback();
            }
            finally
            {
                DbProvider.Transaction.Dispose();
                DbProvider.Transaction = DbProvider.Connection.BeginTransaction();
            }
        }

        public void Dispose()
        {
            DbProvider.Connection.Close();
            DbProvider.Connection.Dispose();
            DbProvider.Transaction.Dispose();
        }
    }
}
