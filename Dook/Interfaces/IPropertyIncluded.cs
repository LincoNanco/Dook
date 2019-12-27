using System.Collections.Generic;
using System.Linq;

namespace Dook
{
    public interface IPropertyIncluded<T> : IQueryable<T>, IEnumerable<T>, IQueryable
    {

    }
}