using System;

namespace Dook.Attributes
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class ForeignKeyAttribute : Attribute
    {
        public string ForeignKey { get; set; }

        public ForeignKeyAttribute(string ForeignKey)
        {
            this.ForeignKey = ForeignKey;
        }
    }
}
