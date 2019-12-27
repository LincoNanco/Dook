using System;

namespace Dook.Attributes
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class ManyToManyAttribute : Attribute
    {
        public Type IntermediateType { get; set; }
        public string ForeignKey { get; set; }
        public string TheOtherForeignKey { get; set; }

        public ManyToManyAttribute(Type IntermediateType, string ForeignKey, string TheOtherForeignKey)
        {
            this.IntermediateType = IntermediateType;
            this.ForeignKey = ForeignKey;
            this.TheOtherForeignKey = TheOtherForeignKey;
        }
    }
}
