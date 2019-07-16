using System;
using System.Collections.Generic;
using System.Linq;
using Dook.Tests.Models;
using Xunit;

namespace Dook.Tests
{
    public class SqlTranslatorTests
    {
        /// <summary>
        /// Testing query translation
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<object[]> QueryTestsData()
        {   
            Query<TestModel> queryObject = new Query<TestModel>(new QueryProvider(new DbProvider(DbType.Sql, "Server=127.0.0.1;Database=fakedb;User Id=FakeUser;Password=fake.password;")));
            IQueryable<TestModel> query = queryObject.Where(t => t.BoolProperty && t.EnumProperty == TestEnum.Three);
            yield return new object[]
            {
                query,
                "SELECT * FROM (SELECT t.[Id], t.[BoolProperty], t.[CreatedOn], t.[DateTimeProperty], t.[EnumProperty], t.[StringProperty], t.[UpdatedOn] FROM TestModels AS t) AS t WHERE (t.[BoolProperty] AND (t.[EnumProperty] = @P0))"
            };
        }
        [Theory, MemberData("QueryTestsData")]
        public void QueryTests(IQueryable<TestModel> query, string expectedResult)
        {
            SQLServerTranslator translator = new SQLServerTranslator();
            SQLPredicate Sql = translator.Translate(query.Expression);
            Assert.Equal(expectedResult, Sql.Sql);
        }   
    }
}
