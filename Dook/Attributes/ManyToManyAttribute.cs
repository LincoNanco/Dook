using System;

namespace Dook.Attributes
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class ManyToManyAttribute : Attribute
    {
        public string IntermediateTable { get; set; }
        public string ForeignKey { get; set; }
        public string TheOtherForeignKey { get; set; }

        public ManyToManyAttribute(string IntermediateTable, string ForeignKey, string TheOtherForeignKey)
        {
            this.IntermediateTable = IntermediateTable;
            this.ForeignKey = ForeignKey;
            this.TheOtherForeignKey = TheOtherForeignKey;
        }
    }
}
