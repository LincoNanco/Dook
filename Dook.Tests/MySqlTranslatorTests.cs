using System;
using System.Linq;
using Xunit;
using Dook.Tests.Models;

namespace Dook.Tests
{
    public class MySqlTranslatorTests
    {
        [Fact]
        public void TestQuery()
        {
            MySQLTranslator translator = new MySQLTranslator();
            Query<TestModel> queryObject = new Query<TestModel>(new QueryProvider(new DbProvider(DbType.Sql, "Server=127.0.0.1;Database=fakedb;User Id=FakeUser;Password=fake.password;")));
            IQueryable<TestModel> query = queryObject.Where(t => t.BoolProperty && t.EnumProperty == TestEnum.Three);
            SQLPredicate Sql = translator.Translate(query.Expression);
            Assert.Equal("SELECT t.Id, t.BoolProperty, t.CreatedOn, t.DateTimeProperty, t.EnumProperty, t.StringProperty, t.UpdatedOn FROM TestModels AS t WHERE (t.BoolProperty AND (t.EnumProperty = @P0))", Sql.Sql);
            Assert.Equal(Sql.Parameters["@P0"],2);
        }  
    }
}
