
using System;
namespace Dook.Attributes
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class SqlQueryAttribute : Attribute
    {
        public string Query { get; set; }
        public Type Type { get; set; }

        public SqlQueryAttribute(string query, Type type)
        {
            Query = String.Format(query, Mapper.GetColumnNames(type));
            Type = type;
        }
    }
}
