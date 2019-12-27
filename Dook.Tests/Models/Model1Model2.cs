using System;
using Dook.Attributes;

namespace Dook.Tests.Models
{
    public class Model1Model2 : IEntity
    {
        public int Id { get; set; }
        [ForeignKey("Id")]
        public ManyToManyModel1 Model1 { get; set; }
        [ForeignKey("Id")]
        public ManyToManyModel2 Model2 { get; set; }
    }
}