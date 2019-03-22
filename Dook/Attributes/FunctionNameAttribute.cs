using System;

namespace Dook.Attributes
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class FunctionNameAttribute : Attribute
    {
        public string FunctionName { get; set; }

        public FunctionNameAttribute(string FunctionName)
        {
            this.FunctionName = FunctionName;
        }
    }
}
