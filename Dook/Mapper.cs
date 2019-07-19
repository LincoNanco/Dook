using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dook;
using Dook.Attributes;

public static class Mapper
{
    public static Dictionary<string,ColumnInfo> GetTableMapping<T>()
    {
        //getting properties in a specific order
        Dictionary<string,ColumnInfo> TableMapping = new Dictionary<string,ColumnInfo>();
        TypeInfo typeInfo = typeof(T).GetTypeInfo();
        List<PropertyInfo> properties = new List<PropertyInfo>(); 
        PropertyInfo idPropertyInfo = typeInfo.GetProperty("Id");
        if (idPropertyInfo != null) properties.Add(typeInfo.GetProperty("Id")) ; //TODO: this is because Join reader always assume Id comes first
        properties.AddRange(typeof(T).GetTypeInfo().GetProperties().Where(p => p.Name != "Id").OrderBy(p => p.Name).ToList());
        foreach (PropertyInfo p in properties)
        {
            NotMappedAttribute nm = p.GetCustomAttribute<NotMappedAttribute>();
            if (nm == null)
            {
                ColumnNameAttribute cma = p.GetCustomAttribute<ColumnNameAttribute>();
                TableMapping.Add(p.Name, new ColumnInfo { ColumnName = cma != null ? cma.ColumnName : p.Name, ColumnType = p.PropertyType });
            }
        }
        return TableMapping;
    }

    /// <summary>
    /// Gets a comma separated string with the column names. Meant to build queries.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static string GetColumnNames(Type type)
    {
        //getting properties in a specific order
        Dictionary<string,string> Mapping = new Dictionary<string,string>();
        TypeInfo typeInfo = type.GetTypeInfo();
        List<PropertyInfo> properties = new List<PropertyInfo>(); 
        PropertyInfo idPropertyInfo = typeInfo.GetProperty("Id");
        if (idPropertyInfo != null) properties.Add(typeInfo.GetProperty("Id")) ; //TODO: this is because Join reader always assume Id comes first
        properties.AddRange(typeInfo.GetProperties().Where(p => p.Name != "Id").OrderBy(p => p.Name).ToList());
        foreach (PropertyInfo p in properties)
        {
            NotMappedAttribute nm = p.GetCustomAttribute<NotMappedAttribute>();
            if (nm == null)
            {
                ColumnNameAttribute cma = p.GetCustomAttribute<ColumnNameAttribute>();
                TableAliasAttribute taa = p.GetCustomAttribute<TableAliasAttribute>();
                if (taa == null) throw new Exception("Table alias must be defined for each mapped attribute.");
                Mapping.Add(p.Name, cma != null ? $"{taa.Alias}.{cma.ColumnName}" : $"{taa.Alias}.{p.Name}");
            }
        }
        return String.Join(", ", Mapping.Values);
    }
}