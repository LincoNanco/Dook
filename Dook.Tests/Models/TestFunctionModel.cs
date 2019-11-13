using System;
using Dook.Attributes;

namespace Dook.Tests.Models
{
    [FunctionName("FakeProcedure")]
    public class TestFunctionModel : IEntityAuditable
    {
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
        public int Id { get; set; }
        public bool BoolProperty { get; set; }
        public DateTime DateTimeProperty { get; set; }
        public string StringProperty { get; set; }
        public TestEnum EnumProperty { get; set; }
        [NotMapped]
        public int NotMappedInt { get; set; }
    }
}