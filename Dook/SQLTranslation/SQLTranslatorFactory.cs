namespace Dook
{
    internal static class SQLTranslatorFactory
    {
        public static ISQLTranslator GetTranslator(DbType dbType)
        {
            switch (dbType)
            {
                case DbType.MySql:
                    return new MySQLTranslator();
                case DbType.Sql:
                    return new SQLServerTranslator();
                default:
                    return new MySQLTranslator();
            }
        }
    }
}