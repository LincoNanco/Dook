using System;
using System.Linq;
using Xunit;
using Dook.Tests.Models;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Dook.Tests
{
    public class MySqlTranslatorTests
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
                "SELECT t.Id, t.BoolProperty, t.CreatedOn, t.DateTimeProperty, t.EnumProperty, t.StringProperty, t.UpdatedOn FROM TestModels AS t WHERE (t.BoolProperty AND (t.EnumProperty = @P0))"
            };
            IQueryable<TestModel> query2 = queryObject.Where(t => !t.BoolProperty && t.EnumProperty == TestEnum.Three);
            yield return new object[]
            {
                query2,
                "SELECT t.Id, t.BoolProperty, t.CreatedOn, t.DateTimeProperty, t.EnumProperty, t.StringProperty, t.UpdatedOn FROM TestModels AS t WHERE ( NOT t.BoolProperty AND (t.EnumProperty = @P0))"
            };
            IQueryable<TestModel> query3 = queryObject.OrderBy(t => t.StringProperty).Where(t => t.EnumProperty == TestEnum.One);
            yield return new object[]
            {
                query3,
                "SELECT * FROM (SELECT t.Id, t.BoolProperty, t.CreatedOn, t.DateTimeProperty, t.EnumProperty, t.StringProperty, t.UpdatedOn FROM TestModels AS t ORDER BY t.StringProperty) AS t WHERE (t.EnumProperty = @P0)"
            };
            IQueryable<TestModel> query4 = queryObject.Where(t => t.StringProperty == "Test").Where(t => t.EnumProperty == TestEnum.One);
            yield return new object[]
            {
                query4,
                "SELECT * FROM (SELECT t.Id, t.BoolProperty, t.CreatedOn, t.DateTimeProperty, t.EnumProperty, t.StringProperty, t.UpdatedOn FROM TestModels AS t WHERE (t.StringProperty = @P0)) AS t WHERE (t.EnumProperty = @P1)"
            };
            IQueryable<TestModel> query5 = queryObject.Where(t => t.StringProperty == "Test").Where(t => t.EnumProperty == TestEnum.One).Take(1);
            yield return new object[]
            {
                query5,
                "SELECT * FROM (SELECT t.Id, t.BoolProperty, t.CreatedOn, t.DateTimeProperty, t.EnumProperty, t.StringProperty, t.UpdatedOn FROM TestModels AS t WHERE (t.StringProperty = @P0)) AS t WHERE (t.EnumProperty = @P1) LIMIT 1"
            };
            IQueryable<TestModel> query6 = queryObject.Where(t => t.StringProperty == "Test").OrderByDescending(t => t.StringProperty).Where(t => t.EnumProperty == TestEnum.One).Take(1);
            yield return new object[]
            {
                query6,
                "SELECT * FROM (SELECT t.Id, t.BoolProperty, t.CreatedOn, t.DateTimeProperty, t.EnumProperty, t.StringProperty, t.UpdatedOn FROM TestModels AS t WHERE (t.StringProperty = @P0) ORDER BY t.StringProperty DESC ) AS t WHERE (t.EnumProperty = @P1) LIMIT 1"
            };
        }
        [Theory, MemberData("QueryTestsData")]
        public void QueryTests(IQueryable<TestModel> query, string expectedResult)
        {
            MySQLTranslator translator = new MySQLTranslator();
            SQLPredicate Sql = translator.Translate(query.Expression);
            Console.WriteLine(Sql.Sql);
            Assert.Equal(expectedResult, Sql.Sql);
        }  

        
        [Fact]
        public void SelectReturningNewObjectTest()
        {
            Query<TestModel> queryObject = new Query<TestModel>(new QueryProvider(new DbProvider(DbType.Sql, "Server=127.0.0.1;Database=fakedb;User Id=FakeUser;Password=fake.password;")));
            IQueryable<TestResult> query = queryObject.Where(t => t.BoolProperty && t.EnumProperty == TestEnum.Three).Select(t => new TestResult { BoolProperty = t.BoolProperty, StringProperty = t.StringProperty } );
            string expectedResult = "SELECT t.BoolProperty, t.StringProperty FROM (SELECT t.Id, t.BoolProperty, t.CreatedOn, t.DateTimeProperty, t.EnumProperty, t.StringProperty, t.UpdatedOn FROM TestModels AS t WHERE (t.BoolProperty AND (t.EnumProperty = @P0))) AS t";
            MySQLTranslator translator = new MySQLTranslator();
            SQLPredicate Sql = translator.Translate(query.Expression);
            Assert.Equal(expectedResult, Sql.Sql);
        } 

        [Fact]
        public void SelectReturningBoolTest()
        {
            Query<TestModel> queryObject = new Query<TestModel>(new QueryProvider(new DbProvider(DbType.Sql, "Server=127.0.0.1;Database=fakedb;User Id=FakeUser;Password=fake.password;")));
            IQueryable<bool> query = queryObject.Where(t => t.BoolProperty && t.EnumProperty == TestEnum.Three).Select(t => t.BoolProperty);
            string expectedResult = "SELECT t.BoolProperty FROM (SELECT t.Id, t.BoolProperty, t.CreatedOn, t.DateTimeProperty, t.EnumProperty, t.StringProperty, t.UpdatedOn FROM TestModels AS t WHERE (t.BoolProperty AND (t.EnumProperty = @P0))) AS t";
            MySQLTranslator translator = new MySQLTranslator();
            SQLPredicate Sql = translator.Translate(query.Expression);
            Assert.Equal(expectedResult, Sql.Sql);
        }  

        [Fact]
        public void SelectReturningStringTest()
        {
            Query<TestModel> queryObject = new Query<TestModel>(new QueryProvider(new DbProvider(DbType.Sql, "Server=127.0.0.1;Database=fakedb;User Id=FakeUser;Password=fake.password;")));
            IQueryable<string> query = queryObject.Where(t => t.BoolProperty && t.EnumProperty == TestEnum.Three).Select(t => t.StringProperty);
            string expectedResult = "SELECT t.StringProperty FROM (SELECT t.Id, t.BoolProperty, t.CreatedOn, t.DateTimeProperty, t.EnumProperty, t.StringProperty, t.UpdatedOn FROM TestModels AS t WHERE (t.BoolProperty AND (t.EnumProperty = @P0))) AS t";
            MySQLTranslator translator = new MySQLTranslator();
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
                $"SELECT * FROM (SELECT {Mapper.GetColumnNames(typeof(TestQuery))} FROM TestModels AS t JOIN TestModels2 as u ON u.[IntProperty] = @F0 AND t.[StringProperty] = @F1) AS x"
            };
            IQueryable<TestQuery> query2 = queryObject.Where(t => t.AString == "Test");
            yield return new object[]
            {
                query2,
                $"SELECT * FROM (SELECT {Mapper.GetColumnNames(typeof(TestQuery))} FROM TestModels AS t JOIN TestModels2 as u ON u.[IntProperty] = @F0 AND t.[StringProperty] = @F1) AS t WHERE (t.StringProperty = @P0)"
            };
        }

        [Theory, MemberData("StringQueryTestsData")]
        public void StringQueryTest(IQueryable<TestQuery> query, string expectedResult)
        {
            MySQLTranslator translator = new MySQLTranslator();
            SQLPredicate Sql = translator.Translate(query.Expression);
            Assert.Equal(expectedResult, Sql.Sql);
        }
    }
}
