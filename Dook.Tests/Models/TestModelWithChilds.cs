using System;
using System.Collections.Generic;
using Dook.Attributes;

namespace Dook.Tests.Models
{
    public class TestModelWithChilds : IEntityAuditable
    {
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
        public int Id { get; set; }
        public bool BoolProperty { get; set; }
        public DateTime DateTimeProperty { get; set; }
        public string StringProperty { get; set; }
        public TestEnum EnumProperty { get; set; }

        [InvertedProperty("TestModelWithChildsId")]
        public List<ChildModel> ChildModels { get; set; }
        
        [NotMapped]
        public int NotMappedInt { get; set; }
    }

}