using System;

namespace Dook.Attributes
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class IsParameterAttribute : Attribute
    {
        public int Index { get; set; }

        public IsParameterAttribute(int Index)
        {
            this.Index = Index;
        }
    }
}
