using System;
using System.Data;
using System.Data.SqlClient;
using MySql.Data.MySqlClient;

namespace Dook
{
    public class DbProvider
    {
        public DbType DbType = DbType.MySql;
        public string ConnectionString = string.Empty;
        public IDbConnection Connection;
        public IDbTransaction Transaction;

        int? CommandTimeout;

        public DbProvider(DbType dbType, string connectionString, int? commandTimeout = null)
        {
            DbType = dbType;
            ConnectionString = connectionString;
            Connection = GetConnection();
            CommandTimeout = commandTimeout;
        }

        public IDbCommand GetCommand()
        {
            IDbCommand DbCommand = null;
            switch (DbType)
            {
                case DbType.Sql:
                    DbCommand = new SqlCommand();
                    DbCommand.Transaction = Transaction;
                    break;
                case DbType.MySql:
                    DbCommand =  new MySqlCommand();
                    break;
                default:
                    throw new Exception("Unsuported database provider.");
            }
            DbCommand.Connection = Connection;
            if (CommandTimeout != null) DbCommand.CommandTimeout = (int) CommandTimeout;
            return DbCommand;
        }

        private IDbConnection GetConnection()
        {
            IDbConnection DbConnection = null;
            switch (DbType)
            {
                case DbType.Sql:
                    DbConnection = new SqlConnection(ConnectionString);
                    break;
                case DbType.MySql:
                    DbConnection = new MySqlConnection(ConnectionString);
                    break;
                default:
                    throw new Exception("Unsuported database provider.");
            }
            return DbConnection;
        }
    }
}
