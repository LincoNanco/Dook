using System;
using System.Linq;
using Dook.Tests.Models;
using Xunit;

namespace Dook
{
    public class SqlTranslatorTests
    {
        [Fact]
        public void TestQuery()
        {
            SQLServerTranslator translator = new SQLServerTranslator();
            Query<TestModel> queryObject = new Query<TestModel>(new QueryProvider(new DbProvider(DbType.Sql, "Server=127.0.0.1;Database=fakedb;User Id=FakeUser;Password=fake.password;")));
            IQueryable<TestModel> query = queryObject.Where(t => t.BoolProperty == true && t.EnumProperty == TestEnum.Three);
            SQLPredicate Sql = translator.Translate(query.Expression);
            Assert.Equal("SELECT * FROM (SELECT t.[Id], t.[BoolProperty], t.[CreatedOn], t.[DateTimeProperty], t.[EnumProperty], t.[StringProperty], t.[UpdatedOn] FROM TestModels AS t) AS t WHERE ((t.[BoolProperty] = @P0) AND (t.[EnumProperty] = @P1))", Sql.Sql);
            Assert.Equal(Sql.Parameters["@P0"],true);
            Assert.Equal(Sql.Parameters["@P1"],2);
        }   
    }
}
