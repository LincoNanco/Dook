using System;

namespace Dook.Tests.Models
{
    public class TestModel : IEntityAuditable
    {
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
        public int Id { get; set; }
        public bool BoolProperty { get; set; }
        public DateTime DateTimeProperty { get; set; }
        public string StringProperty { get; set; }
        public TestEnum EnumProperty { get; set; }
    }

    public enum TestEnum
    {
        One,
        Two,
        Three
    }
}