using System.Collections.Generic;
using System.Data;
using FastMember;

namespace Dook.Extensions
{
    public static class DataTableExtensions
    {
        public static DataTable ToDataTable<T>(this IEnumerable<T> items, TypeAccessor accessor, Dictionary<string,ColumnInfo> tableMapping)
        {
            DataTable tb = new DataTable(typeof(T).Name);
            foreach(ColumnInfo prop in tableMapping.Values)
            {
                tb.Columns.Add(prop.ColumnName, prop.ColumnType);
            }
            foreach (T item in items)
            {
                DataRow row = tb.NewRow();
                foreach (KeyValuePair<string, ColumnInfo> kvp in tableMapping)
                {
                    row[kvp.Value.ColumnName] = accessor[item, kvp.Key];
                }
                tb.Rows.Add(row);
            }
            return tb;
        }
    }
}