using System;
using System.Linq;
using Xunit;
using Dook.Tests.Models;
using System.Data;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Dook.Tests
{
    public class QueryProviderTests
    {
        /// <summary>
        /// DeleteWhere tests
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<object[]> GetSqlDeleteWhereTestsData()
        {
            Expression<Func<TestModel,bool>> e1 = t => t.StringProperty.Contains("Test");
            yield return new object[]
            {
                e1,
                "DELETE FROM TestModels WHERE [StringProperty] LIKE @P0;"
            };
            Expression<Func<TestModel,bool>> e2 = t => t.Id > 2 && t.CreatedOn < new DateTime(2019,05,05);
            yield return new object[]
            {
                e2,
                "DELETE FROM TestModels WHERE (([Id] > @P0) AND ([CreatedOn] < @P1));"
            };
        }

        [Theory, MemberData("GetSqlDeleteWhereTestsData")]
        public void GetSqlDeleteWhereTest(Expression<Func<TestModel,bool>> expression, string expectedResult)
        {
            QueryProvider provider = new QueryProvider(new DbProvider(DbType.Sql, "Server=127.0.0.1;Database=fakedb;User Id=FakeUser;Password=fake.password;"));
            IDbCommand cmd = provider.GetDeleteWhereCommand<TestModel>(expression, "TestModels");
            Assert.Equal(expectedResult, cmd.CommandText);
        } 

               /// <summary>
        /// DeleteWhere tests
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<object[]> GetMySqlDeleteWhereTestsData()
        {
            Expression<Func<TestModel,bool>> e1 = t => t.StringProperty.Contains("Test");
            yield return new object[]
            {
                e1,
                "DELETE FROM TestModels WHERE StringProperty LIKE @P0;"
            };
            Expression<Func<TestModel,bool>> e2 = t => t.Id > 2 && t.CreatedOn < new DateTime(2019,05,05);
            yield return new object[]
            {
                e2,
                "DELETE FROM TestModels WHERE ((Id > @P0) AND (CreatedOn < @P1));"
            };
        }
        
        [Theory, MemberData("GetMySqlDeleteWhereTestsData")]
        public void GetMySqlDeleteWhereTest(Expression<Func<TestModel,bool>> expression, string expectedResult)
        {
            QueryProvider provider = new QueryProvider(new DbProvider(DbType.MySql, "Server=127.0.0.1;Database=fakedb;User Id=FakeUser;Password=fake.password;"));
            IDbCommand cmd = provider.GetDeleteWhereCommand<TestModel>(expression, "TestModels");
            Assert.Equal(expectedResult, cmd.CommandText);
        } 

        /// <summary>
        /// DeleteAll tests
        /// </summary>
        [Fact]
        public void GetDeleteAllTest()
        {
            MySQLTranslator translator = new MySQLTranslator();
            QueryProvider provider = new QueryProvider(new DbProvider(DbType.Sql, "Server=127.0.0.1;Database=fakedb;User Id=FakeUser;Password=fake.password;"));
            IDbCommand cmd = provider.GetDeleteAllCommand("TestModels");
            Assert.Equal("DELETE FROM TestModels;", cmd.CommandText);
        } 

        /// <summary>
        /// GetDeleteCommand Test 
        /// </summary>
        /// <param name="model"></param>
        [Fact]
        public void GetDeleteCommandTest()
        {
            TestModel model = new TestModel{ Id = 1 };
            MySQLTranslator translator = new MySQLTranslator();
            QueryProvider provider = new QueryProvider(new DbProvider(DbType.Sql, "Server=127.0.0.1;Database=fakedb;User Id=FakeUser;Password=fake.password;"));
            IDbCommand cmd = provider.GetDeleteCommand(model.Id, "TestModels", Mapper.GetTableMapping<TestModel>());
            Assert.Equal("DELETE FROM TestModels WHERE Id = @id;", cmd.CommandText);
        }

        /// <summary>
        /// GetUpdateCommand Tests
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<object[]> GetUpdateCommandTestsData()
        {
            List<string> properties = new List<string>();
            Dictionary<string, ColumnInfo> TableMapping = Mapper.GetTableMapping<TestModel>();
            foreach (string attributeName in TableMapping.Keys)
            {
                if (attributeName == "Id" || attributeName == "CreatedOn") continue;
                properties.Add($"{TableMapping[attributeName].ColumnName} = @{attributeName}");
            }
            yield return new object[]
            {
                new TestModel{
                    CreatedOn = new DateTime(2019,05,07),
                    UpdatedOn = new DateTime(2019,05,07),
                    Id = 1,
                    BoolProperty = true,
                    DateTimeProperty = new DateTime(2019,05,07),
                    StringProperty = "test",
                    EnumProperty = TestEnum.One
                },
                $"UPDATE TestModels SET {String.Join(", ", properties)} WHERE Id = @id;"
            };
            yield return new object[]
            {
                new TestModel{
                    CreatedOn = new DateTime(2019,05,07),
                    UpdatedOn = new DateTime(2019,05,07),
                    Id = 0,
                    BoolProperty = true,
                    DateTimeProperty = new DateTime(2019,05,07),
                    StringProperty = "test",
                    EnumProperty = TestEnum.One
                },
                "Id property must be a positive integer."
            };
        }

        [Theory, MemberData("GetUpdateCommandTestsData")]
        public void GetUpdateCommandTests(TestModel model, string expectedResult)
        {
            MySQLTranslator translator = new MySQLTranslator();
            QueryProvider provider = new QueryProvider(new DbProvider(DbType.Sql, "Server=127.0.0.1;Database=fakedb;User Id=FakeUser;Password=fake.password;"));
            string result;
            try
            {
                IDbCommand cmd = provider.GetUpdateCommand(model, "TestModels", Mapper.GetTableMapping<TestModel>());
                result = cmd.CommandText;
            }
            catch (Exception e)
            {
                result = e.Message;
            }
            Assert.Equal(expectedResult, result);
        }

        /// <summary>
        /// GetInsertCommand Tests
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<object[]> GetInsertCommandTestsData()
        {
            List<string> properties = new List<string>();
            List<string> values = new List<string>();
            Dictionary<string, ColumnInfo> TableMapping = Mapper.GetTableMapping<TestModel>();
            foreach (string attributeName in TableMapping.Keys)
            {
                if (attributeName == "Id") continue;
                properties.Add($"{TableMapping[attributeName].ColumnName}");
                values.Add($"@{attributeName}");
            }
            yield return new object[]
            {
                new TestModel{
                    CreatedOn = new DateTime(2019,05,07),
                    UpdatedOn = new DateTime(2019,05,07),
                    Id = 1,
                    BoolProperty = true,
                    DateTimeProperty = new DateTime(2019,05,07),
                    StringProperty = "test",
                    EnumProperty = TestEnum.One
                },
                $"INSERT INTO TestModels ({String.Join(", ", properties)}) VALUES ({String.Join(", ", values)}); SELECT @@IDENTITY;"
            };
        }

        [Theory, MemberData("GetInsertCommandTestsData")]
        public void GetInsertCommandTests(TestModel model, string expectedResult)
        {
            MySQLTranslator translator = new MySQLTranslator();
            QueryProvider provider = new QueryProvider(new DbProvider(DbType.Sql, "Server=127.0.0.1;Database=fakedb;User Id=FakeUser;Password=fake.password;"));
            string result;
            IDbCommand cmd = provider.GetInsertCommand(model, "TestModels", Mapper.GetTableMapping<TestModel>());
            result = cmd.CommandText;
            Assert.Equal(expectedResult, result);
        }
    }
}
