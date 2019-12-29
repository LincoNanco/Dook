using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using Dook.Attributes;
using FastMember;

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
        public static T GetEntityUsingIndex<T>(IDataReader oReader, int position, Dictionary<string, ColumnInfo> TableMapping, TypeAccessor accessor) where T : IEntity, new()
        {
            if (TableMapping.Count == 0) throw new ArgumentException("Table Mapping not set!!!");
            T entity = new T();
            int i = position;
            //Returning default object of type T when no Id is reported for an entity
            if (oReader[i] == DBNull.Value)
            {
                return default(T);
            }
            foreach (string p in TableMapping.Keys)
            {
                object value = oReader[i];
                if (value != DBNull.Value)
                {
                    if (value != null)
                    {
                        accessor[entity,p] = ChangeType(value, TableMapping[p].ColumnType);
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
        Dictionary<string, ColumnInfo> TableMapping;

        internal ObjectReader(IDataReader Reader, SQLPredicate predicate)
        {
            TableMapping = predicate.TableMappings[typeof(T).Name];
            enumerator = new Enumerator(Reader, predicate);
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
            SQLPredicate _predicate;
            Dictionary<Type, Dictionary<int, object>> readEntityTracker = new Dictionary<Type, Dictionary<int, object>>();
            Dictionary<Type, TypeAccessor> typeAccessors = new Dictionary<Type, TypeAccessor>();
            Dictionary<string, int> startIndex = new Dictionary<string, int>();
            Dictionary<string, int> endIndex = new Dictionary<string, int>();
            Dictionary<Type, MethodInfo> addToListAccessors = new Dictionary<Type, MethodInfo>();

            public Enumerator(IDataReader oReader, SQLPredicate predicate)
            {
                reader = oReader;
                _predicate = predicate;
                typeAccessors.Add(typeof(T), TypeAccessor.Create(typeof(T)));
                //determining start and end indexes
                int start = 0;
                foreach(KeyValuePair<string,Dictionary<string, ColumnInfo>> dict in _predicate.TableMappings)
                {
                    startIndex.Add(dict.Key, start);
                    endIndex.Add(dict.Key, start + dict.Value.Count);
                    start += dict.Value.Count;
                }
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
                    ReadObject(entity, reader, 0, _predicate.TableMappings[typeof(T).Name].Count - 1);
                    return true;
                }
                return false;
            }

            private void ReadObject(T entity, IDataReader reader, int start, int end)
            {
                //if this entity is already read, skip reading all its attributes except from ByRef ones
                bool alreadyRead = false;
                if (entity is IEntity)
                {
                    int entityId = Convert.ToInt32(reader[start]);
                    alreadyRead = readEntityTracker[typeof(T)].ContainsKey(entityId); 
                    if (alreadyRead) entity = (T) readEntityTracker[typeof(T)][entityId];
                }
                int i = start;
                Dictionary<string, ColumnInfo> tableMapping = _predicate.TableMappings[typeof(T).Name];
                TypeAccessor _accessor = typeAccessors[typeof(T)];
                foreach (string p in tableMapping.Keys)
                {
                    if (i > end) break;
                    //Checking if property is reference type. If so, reading another object to build it.
                    if (tableMapping[p].ColumnType.IsValueType && !alreadyRead)
                    {
                        object value = reader[i];
                        if (value != DBNull.Value)
                        {
                            if (value != null)
                            {
                                _accessor[entity, p] = ChangeType(value, tableMapping[p].ColumnType);
                            }
                        }
                        i++;
                    }
                    else if (tableMapping[p].ColumnType.IsByRef)
                    {
                        //If is a List, check if the list is instanced and add the new object to the list
                        if (tableMapping[p].ColumnType.IsGenericType && (tableMapping[p].ColumnType.GetGenericTypeDefinition() == typeof(List<>)))
                        {
                            //if the list is not instanced, instance it
                            if (_accessor[entity, p] == null)
                            {
                                object newList = Expression.Lambda<Func<object>>(Expression.Convert(Expression.New(tableMapping[p].ColumnType), typeof(object))).Compile()();
                                //saving an accessor to Add method of this list, to avoid using reflection again to get it
                                addToListAccessors.Add(tableMapping[p].ColumnType,tableMapping[p].ColumnType.GetMethod("Add"));
                            }
                            addToListAccessors[tableMapping[p].ColumnType].Invoke(_accessor[entity,p], new object[] { ReadObject(tableMapping[p].ColumnType, reader, startIndex[tableMapping[p].ColumnType.Name], endIndex[tableMapping[p].ColumnType.Name]) });
                            continue;
                        }
                        //If is an object, read it.
                        _accessor[entity, p] = ReadObject(tableMapping[p].ColumnType, reader, startIndex[tableMapping[p].ColumnType.Name], endIndex[tableMapping[p].ColumnType.Name]);
                        continue;
                    }
                    else
                    {
                        throw new Exception($"Reading an object of type {tableMapping[p].ColumnType.Name} is not supported.");
                    }
                }
                current = entity;
            }

            /// <summary>
            /// Returns a new object read from an open reader, based on an initial and ending read index
            /// </summary>
            /// <param name="objectType"></param>
            /// <param name="reader"></param>
            /// <param name="start"></param>
            /// <param name="end"></param>
            /// <returns></returns>
            /// TODO: implement this
            private object ReadObject(Type objectType, IDataReader reader, int start, int end)
            {
                //if this entity is already read, skip reading all its attributes except from ByRef ones
                bool alreadyRead = false;
                object newObject;
                if (objectType.IsAssignableFrom(typeof(IEntity)))
                {
                    int entityId = Convert.ToInt32(reader[start]);
                    alreadyRead = readEntityTracker[objectType].ContainsKey(entityId); 
                    if (alreadyRead)
                    {
                        newObject = readEntityTracker[objectType][entityId];
                    } 
                    else
                    {
                        newObject = Expression.Lambda<Func<object>>(Expression.Convert(Expression.New(objectType), typeof(object))).Compile()();
                    }
                }
                else
                {
                    newObject = Expression.Lambda<Func<object>>(Expression.Convert(Expression.New(objectType), typeof(object))).Compile()();
                }
                int i = start;
                Dictionary<string, ColumnInfo> tableMapping = _predicate.TableMappings[typeof(T).Name];
                TypeAccessor _accessor = typeAccessors[typeof(T)];
                foreach (string p in tableMapping.Keys)
                {
                    if (i > end) break;
                    //Checking if property is reference type. If so, reading another object to build it.
                    if (tableMapping[p].ColumnType.IsValueType && !alreadyRead)
                    {
                        object value = reader[i];
                        if (value != DBNull.Value)
                        {
                            if (value != null)
                            {
                                _accessor[newObject, p] = ChangeType(value, tableMapping[p].ColumnType);
                            }
                        }
                        i++;
                    }
                    else if (tableMapping[p].ColumnType.IsByRef)
                    {
                        //If is a List, check if the list is instanced and add the new object to the list
                        if (tableMapping[p].ColumnType.IsGenericType && (tableMapping[p].ColumnType.GetGenericTypeDefinition() == typeof(List<>)))
                        {
                            //if the list is not instanced, instance it
                            if (_accessor[newObject, p] == null)
                            {
                                object newList = Expression.Lambda<Func<object>>(Expression.Convert(Expression.New(tableMapping[p].ColumnType), typeof(object))).Compile()();
                                //saving an accessor to Add method of this list, to avoid using reflection again to get it
                                addToListAccessors.Add(tableMapping[p].ColumnType,tableMapping[p].ColumnType.GetMethod("Add"));
                            }
                            addToListAccessors[tableMapping[p].ColumnType].Invoke(_accessor[newObject,p], new object[] { ReadObject(tableMapping[p].ColumnType, reader, startIndex[tableMapping[p].ColumnType.Name], endIndex[tableMapping[p].ColumnType.Name]) });
                            continue;
                        }
                        //If is an object, read it.
                        _accessor[newObject, p] = ReadObject(tableMapping[p].ColumnType, reader, startIndex[tableMapping[p].ColumnType.Name], endIndex[tableMapping[p].ColumnType.Name]);
                        continue;
                    }
                    else
                    {
                        throw new Exception($"Reading an object of type {tableMapping[p].ColumnType.Name} is not supported.");
                    }
                }
                return newObject;
            }

            public void Reset()
            {

            }
        }
    }
}
