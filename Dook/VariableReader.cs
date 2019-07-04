using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;

namespace Dook
{
    /// <summary>
    /// This class is needed to read different kinds of primitive variables from an opened IDataReader.
    /// </summary>
    public class VariableReader<T> : IEnumerable<T>, IEnumerable
    {
        Enumerator enumerator;

        internal VariableReader(IDataReader Reader)
        {
            enumerator = new Enumerator(Reader);
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

            public Enumerator(IDataReader oReader)
            {
                reader = oReader;
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
                    int i = 0;
                    object value = reader[i];
                    if (value != DBNull.Value)
                    {
                        if (value != null)
                        {
                            current = (T)ChangeType(value, typeof(T));
                        }
                    }
                    i++;
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
