using System;

namespace Dook.Attributes
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class TableNameAttribute : Attribute
    {
        public string TableName { get; set; }

        public TableNameAttribute(string TableName)
        {
            this.TableName = TableName;
        }
    }
}
