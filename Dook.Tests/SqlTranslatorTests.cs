using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
            IQueryable<TestModel> query = queryObject.Where(t => t.BoolProperty == true && t.EnumProperty == TestEnum.Three);
            yield return new object[]
            {
                query,
                "SELECT * FROM (SELECT t.[Id], t.[BoolProperty], t.[CreatedOn], t.[DateTimeProperty], t.[EnumProperty], t.[StringProperty], t.[UpdatedOn] FROM TestModels AS t) AS t WHERE ((t.[BoolProperty] = @P0) AND (t.[EnumProperty] = @P1))"
            };
        }
        [Theory, MemberData("QueryTestsData")]
        public void QueryTests(IQueryable<TestModel> query, string expectedResult)
        {
            SQLServerTranslator translator = new SQLServerTranslator();
            SQLPredicate Sql = translator.Translate(query.Expression);
            Assert.Equal(expectedResult, Sql.Sql);
        }

        [Fact]
        public void SelectReturningNewObjectTest()
        {
            Query<TestModel> queryObject = new Query<TestModel>(new QueryProvider(new DbProvider(DbType.Sql, "Server=127.0.0.1;Database=fakedb;User Id=FakeUser;Password=fake.password;")));
            IQueryable<TestResult> query = queryObject.Where(t => t.BoolProperty == true && t.EnumProperty == TestEnum.Three).Select(t => new TestResult { BoolProperty = t.BoolProperty, StringProperty = t.StringProperty } );
            string expectedResult = "SELECT t.[BoolProperty], t.[StringProperty] FROM (SELECT * FROM (SELECT t.[Id], t.[BoolProperty], t.[CreatedOn], t.[DateTimeProperty], t.[EnumProperty], t.[StringProperty], t.[UpdatedOn] FROM TestModels AS t) AS t WHERE ((t.[BoolProperty] = @P0) AND (t.[EnumProperty] = @P1))) AS t";
            SQLServerTranslator translator = new SQLServerTranslator();
            SQLPredicate Sql = translator.Translate(query.Expression);
            Assert.Equal(expectedResult, Sql.Sql);
        } 

        [Fact]
        public void SelectReturningBoolTest()
        {
            Query<TestModel> queryObject = new Query<TestModel>(new QueryProvider(new DbProvider(DbType.Sql, "Server=127.0.0.1;Database=fakedb;User Id=FakeUser;Password=fake.password;")));
            IQueryable<bool> query = queryObject.Where(t => t.BoolProperty == true && t.EnumProperty == TestEnum.Three).Select(t => t.BoolProperty);
            string expectedResult = "SELECT t.[BoolProperty] FROM (SELECT * FROM (SELECT t.[Id], t.[BoolProperty], t.[CreatedOn], t.[DateTimeProperty], t.[EnumProperty], t.[StringProperty], t.[UpdatedOn] FROM TestModels AS t) AS t WHERE ((t.[BoolProperty] = @P0) AND (t.[EnumProperty] = @P1))) AS t";
            SQLServerTranslator translator = new SQLServerTranslator();
            SQLPredicate Sql = translator.Translate(query.Expression);
            Assert.Equal(expectedResult, Sql.Sql);
        }  

        [Fact]
        public void SelectReturningStringTest()
        {
            Query<TestModel> queryObject = new Query<TestModel>(new QueryProvider(new DbProvider(DbType.Sql, "Server=127.0.0.1;Database=fakedb;User Id=FakeUser;Password=fake.password;")));
            IQueryable<string> query = queryObject.Where(t => t.BoolProperty == true && t.EnumProperty == TestEnum.Three).Select(t => t.StringProperty);
            string expectedResult = "SELECT t.[StringProperty] FROM (SELECT * FROM (SELECT t.[Id], t.[BoolProperty], t.[CreatedOn], t.[DateTimeProperty], t.[EnumProperty], t.[StringProperty], t.[UpdatedOn] FROM TestModels AS t) AS t WHERE ((t.[BoolProperty] = @P0) AND (t.[EnumProperty] = @P1))) AS t";
            SQLServerTranslator translator = new SQLServerTranslator();
            SQLPredicate Sql = translator.Translate(query.Expression);
            Assert.Equal(expectedResult, Sql.Sql);
        }

        /// <summary>
        /// Testing query translation
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<object[]> StringQueryTestsData()
        {   
            QueryString<TestQuery> queryObject = new QueryString<TestQuery>(new QueryProvider(new DbProvider(DbType.Sql, "Server=127.0.0.1;Database=fakedb;User Id=FakeUser;Password=fake.password;")));
            queryObject.SQLPredicate.Parameters.Add("@F0", 1);
            queryObject.SQLPredicate.Parameters.Add("@F1", "Test");
            IQueryable<TestQuery> query = queryObject;
            yield return new object[]
            {
                query,
                $"SELECT {Mapper.GetColumnNames(typeof(TestQuery))} FROM TestModels AS t JOIN TestModels2 as u ON u.[IntProperty] = @F0 AND t.[StringProperty] = @F1"
            };
            IQueryable<TestQuery> query2 = queryObject.Where(t => t.AString == "Test");
            yield return new object[]
            {
                query2,
                $"SELECT * FROM (SELECT {Mapper.GetColumnNames(typeof(TestQuery))} FROM TestModels AS t JOIN TestModels2 as u ON u.[IntProperty] = @F0 AND t.[StringProperty] = @F1) AS t WHERE (t.[StringProperty] = @P0)"
            };
        }

        [Theory, MemberData("StringQueryTestsData")]
        public void StringQueryTest(IQueryable<TestQuery> query, string expectedResult)
        {
            SQLServerTranslator translator = new SQLServerTranslator();
            SQLPredicate Sql = translator.Translate(query.Expression);
            Assert.Equal(expectedResult, Sql.Sql);
        }
    }
}
