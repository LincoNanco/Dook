using System;
namespace Dook.Attributes
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class TableAliasAttribute : Attribute
    {
        public string Alias { get; set; }

        public TableAliasAttribute(string alias)
        {
            Alias = alias;
        }
    }
}
