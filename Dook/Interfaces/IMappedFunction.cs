using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Dook
{
    public interface IMappedFunction<T> : IQueryable<T>, IEnumerable<T>, IOrderedQueryable<T>, IMappedFunction
    {
    }

    public interface IMappedFunction : IQueryable, IEnumerable, IOrderedQueryable
    {
        Dictionary<int, string> IndexedParameters { get; }
        string FunctionName { get; }
        Dictionary<string, ColumnInfo> TableMapping { get; }
    }
}
