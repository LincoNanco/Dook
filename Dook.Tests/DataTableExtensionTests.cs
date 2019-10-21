using System.Collections.Generic;
using Dook.Tests.Models;
using Xunit;
using Dook.Extensions;
using FastMember;
using System.Data;

namespace Dook.Tests
{
    public class DataTableExtensionTests
    {
        [Fact]
        public void ToDataTableTest()
        {
            Dictionary<string, ColumnInfo> tableMapping = Mapper.GetTableMapping<TestModel>();
            TypeAccessor accessor = TypeAccessor.Create(typeof(TestModel));
            List<TestModel> testModels = new List<TestModel>{
                new TestModel
                {
                    Id = 1,
                    BoolProperty = true,
                    StringProperty = "Test1"
                },
                new TestModel
                {
                    Id = 2,
                    BoolProperty = false,
                    StringProperty = "Test2"
                },
                new TestModel
                {
                    Id = 3,
                    BoolProperty = true,
                    StringProperty = "Test3"
                }
            };
            DataTable dt = testModels.ToDataTable<TestModel>(accessor, tableMapping);
            Assert.Equal(7, dt.Columns.Count);
        } 
    }
}
