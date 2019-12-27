using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dook;
using Dook.Attributes;

public static class Mapper
{
    public static string GetTableName<T>()
    {
        return GetTableName(typeof(T));
    }
    public static string GetTableName(Type type)
    {
        TableNameAttribute tableNameAtt = type.GetTypeInfo().GetCustomAttribute<TableNameAttribute>();
        return tableNameAtt != null ? tableNameAtt.TableName : type.Name + "s";
    }

    public static Dictionary<string,ColumnInfo> GetTableMapping<T>()
    {
        return GetTableMapping(typeof(T));
    }

    public static Dictionary<string, ColumnInfo> GetTableMapping(Type type)
    {
        //getting properties in a specific order
        Dictionary<string,ColumnInfo> TableMapping = new Dictionary<string,ColumnInfo>();
        TypeInfo typeInfo = type.GetTypeInfo();
        List<PropertyInfo> properties = new List<PropertyInfo>(); 
        PropertyInfo idPropertyInfo = typeInfo.GetProperty("Id");
        if (idPropertyInfo != null) properties.Add(typeInfo.GetProperty("Id")) ; //TODO: this is because Join reader always assume Id comes first
        properties.AddRange(type.GetTypeInfo().GetProperties().Where(p => p.Name != "Id").OrderBy(p => p.Name).ToList());
        foreach (PropertyInfo p in properties)
        {
            NotMappedAttribute nm = p.GetCustomAttribute<NotMappedAttribute>();
            ManyToManyAttribute mtm = p.GetCustomAttribute<ManyToManyAttribute>();
            if (nm == null && mtm == null)
            {
                ColumnNameAttribute cma = p.GetCustomAttribute<ColumnNameAttribute>();
                ForeignKeyAttribute fka = p.GetCustomAttribute<ForeignKeyAttribute>();
                if (fka == null)
                {
                    TableMapping.Add(p.Name, new ColumnInfo { ColumnName = cma != null ? cma.ColumnName : p.Name, ColumnType = p.PropertyType });
                }
                else
                {
                    TableMapping.Add(p.Name, new ColumnInfo { ColumnName = cma != null ? cma.ColumnName : $"{p.Name}Id", ColumnType = p.PropertyType });
                }
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