using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using Dook.Attributes;

namespace Dook
{
    public static class ObjectReader
    {
        /// <summary>
        /// Gets an entity from an open IDataReader using a starting Index. This method is used for Joins.
        /// </summary>
        /// <returns>The entity using index.</returns>
        /// <param name="oReader">O reader.</param>
        /// <param name="position">Position.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public static T GetEntityUsingIndex<T>(IDataReader oReader, int position, Dictionary<string, string> TableMapping) where T : IEntity, new()
        {
            if (TableMapping.Count == 0) throw new ArgumentException("Table Mapping not set!!!");
            T entity = new T();
            Type Type = entity.GetType();
            int i = position;
            //Returning default object of type T when no Id is reported for an entity
            if (oReader[i] == DBNull.Value)
            {
                return default(T);
            }
            foreach (string p in TableMapping.Keys)
            {
                PropertyInfo propertyInfo = Type.GetProperty(p);
                object value = oReader[i];
                if (value != DBNull.Value)
                {
                    if (value != null)
                    {
                        propertyInfo.SetValue(entity, ChangeType(value, propertyInfo.PropertyType));
                    }
                }
                i++;
            }
            return entity;
        }

        /// <summary>
        /// Converts from when type to another and supports conversions to nullable types.
        /// </summary>
        /// <returns>The type.</returns>
        /// <param name="value">Value.</param>
        /// <param name="conversion">Conversion.</param>
        private static object ChangeType(object value, Type conversion)
        {
            var t = conversion;
            if (t.IsGenericType && t.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
            {
                if (value == null)
                {
                    return null;
                }
                t = Nullable.GetUnderlyingType(t);
            }

            //This handles int conversion to Enum when it applies
            if (t.BaseType == typeof(Enum))
            {
                return Enum.ToObject(t, value);
            }
            return Convert.ChangeType(value, t);
        }
    }
    

    /// <summary>
    /// This class is needed to read different kinds of objects from an opened IDataReader.
    /// </summary>
    public class ObjectReader<T> : IEnumerable<T>, IEnumerable where T : class, new()
    {
        Enumerator enumerator;
        Dictionary<string, string> TableMapping;

        internal ObjectReader(IDataReader Reader)
        {
            TableMapping = Mapper.GetTableMapping<T>();
            enumerator = new Enumerator(Reader, TableMapping);
        }

        /// <summary>
        /// Converts from when type to another and supports conversions to nullable types.
        /// </summary>
        /// <returns>The type.</returns>
        /// <param name="value">Value.</param>
        /// <param name="conversion">Conversion.</param>
        private static object ChangeType(object value, Type conversion)
        {
            var t = conversion;
            if (t.IsGenericType && t.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
            {
                if (value == null)
                {
                    return null;
                }
                t = Nullable.GetUnderlyingType(t);
            }

            //This handles int conversion to Enum when it applies
            if (t.BaseType == typeof(Enum))
            {
                return Enum.ToObject(t, value);
            }
            return Convert.ChangeType(value, t);
        }

        public IEnumerator<T> GetEnumerator()
        {
            Enumerator e = enumerator;
            if (e == null)
            {
                throw new InvalidOperationException("Cannot enumerate more than once");
            }enumerator = null;
            return e;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        class Enumerator : IEnumerator<T>, IEnumerator, IDisposable
        {
            IDataReader reader;
            T current;
            Dictionary<string, string> tableMapping;

            public Enumerator(IDataReader oReader, Dictionary<string,string> TableMapping)
            {
                reader = oReader;
                tableMapping = TableMapping;
            }

            public T Current
            {
                get { return current; }
            }

            object IEnumerator.Current
            {
                get { return current; }
            }

            public void Dispose()
            {
                reader.Dispose();
            }

            public bool MoveNext()
            {
                if (reader.Read())
                {
                    T entity = new T();
                    Type Type = entity.GetType();
                    int i = 0;
                    foreach (string p in tableMapping.Keys)
                    {
                        PropertyInfo propertyInfo = Type.GetProperty(p);
                        object value = reader[i];
                        if (value != DBNull.Value)
                        {
                            if (value != null)
                            {
                                propertyInfo.SetValue(entity, ChangeType(value, propertyInfo.PropertyType));
                            }
                        }
                        i++;
                    }
                    current = entity;
                    return true;
                }
                return false;
            }

            public void Reset()
            {

            }
        }
    }
}
