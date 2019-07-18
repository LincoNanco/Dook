using System;
namespace Dook.Attributes
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class ColumnNameAttribute : Attribute
    {
        public string ColumnName { get; set; }

        public ColumnNameAttribute(string columnName)
        {
            this.ColumnName = columnName;
        }
    }
}
