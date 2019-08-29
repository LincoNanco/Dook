using System;
using System.Linq.Expressions;

namespace Dook
{
    public interface ISQLTranslator
    {
        SQLPredicate Translate(Expression expression, int initial = 0, bool ignoreAliases = false);
    }
}
