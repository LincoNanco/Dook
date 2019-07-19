using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Dook
{
    public interface IMappedStringQueryable<T> : IQueryable<T>, IEnumerable<T>, IOrderedQueryable<T>, IMappedStringQueryable
    {

    }

    public interface IMappedStringQueryable : IQueryable, IEnumerable, IOrderedQueryable
    {
        Dictionary<string, ColumnInfo> TableMapping { get; }
        string TableName { get; }
        
        SQLPredicate SQLPredicate { get; set; }
    }
}
