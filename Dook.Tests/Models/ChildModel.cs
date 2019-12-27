using System;
using Dook.Attributes;

namespace Dook.Tests.Models
{
    public class ChildModel : IEntity
    {
        public int Id { get; set; }
        [ForeignKey("Id")]
        [ColumnName("TestModelId")]
        public TestModelWithChilds TestModelProp { get; set; }
        public bool BoolProperty { get; set; }

        [NotMapped]
        public int NotMappedInt { get; set; }
    }

}