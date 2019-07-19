using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Dook
{
    public interface IMappedQueryable<T> : IQueryable<T>, IEnumerable<T>, IOrderedQueryable<T>, IMappedQueryable
    {

    }

    public interface IMappedQueryable : IQueryable, IEnumerable, IOrderedQueryable
    {
        Dictionary<string, ColumnInfo> TableMapping { get; }
        string TableName { get; }
    }
}
