using System;
using System.Collections.Generic;
using Dook.Attributes;

namespace Dook.Tests.Models
{
    public class ManyToManyModel1 : IEntity
    {
        public int Id { get; set; }
        [ManyToMany(typeof(Model1Model2), "Model1Id", "Model2Id")]
        public List<ManyToManyModel2> ManyToManyModel2 { get; set; }
    }
}