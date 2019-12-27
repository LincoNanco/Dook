using System;

namespace Dook.Attributes
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class InvertedPropertyAttribute : Attribute
    {
        public string PropertyName { get; set; }

        public InvertedPropertyAttribute(string PropertyName)
        {
            this.PropertyName = PropertyName;
        }
    }
}
