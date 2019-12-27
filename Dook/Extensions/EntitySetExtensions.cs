using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Dook
{
    public static class EntitySetExtensions
    {
        public static IQueryable<T> Include<T,TKey>(this IQueryable<T> source, Expression<Func<T,TKey>> selector) where T : class, IEntity, new()
        {
            // return source.Provider.CreateQuery<T>(Expression.Call(typeof(EntitySet<T>), "Include", new Type[] { source.ElementType });
            MethodInfo methodInfo = typeof(EntitySet<T>).GetMethod("Include", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo method = methodInfo.MakeGenericMethod(typeof(TKey));
            return source.Provider.CreateQuery<T>(Expression.Call(Expression.Constant(source), method, Expression.Constant(source), selector));
        }
 
        /// <summary>
        /// Call this method to include child object data retrieved from database.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static IMappedQueryable<T> ThenInclude<T, TKey>(this IPropertyIncluded<T> entitySet, Expression<Func<T,TKey>> selector)  where T : class, IEntity, new()
        {
            return (IMappedQueryable<T>) entitySet;
        }
    }
}