using System;
using System.Collections.Generic;
using Dook.Attributes;

namespace Dook.Tests.Models
{
    public class ManyToManyModel2 : IEntity
    {
        public int Id { get; set; }
        [ManyToMany(typeof(Model1Model2), "Model2", "Model1")]
        public List<ManyToManyModel1> ManyToManyModel1 { get; set; }
    }
}