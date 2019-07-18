using System;
using Dook.Attributes;

namespace Dook.Tests.Models
{
    [SqlQuery("SELECT {0} FROM TestModels AS t JOIN TestModels2 as u ON u.[IntProperty] = @F0 AND t.[StringProperty] = @F1", typeof(TestQuery))]
    public class TestQuery
    {
        [TableAlias("u")]
        [ColumnName("IntProperty")]
        public int AnInt { get; set; }
        [TableAlias("t")]
        [ColumnName("StringProperty")]
        public string AString { get; set; }
    }
}