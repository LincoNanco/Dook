using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Transactions;
using Dook;
using System.Linq.Expressions;
using FastMember;

namespace Dook
{
    public class EntitySet<T> : Query<T> where T : class, IEntity, new()
    {
        public Dictionary<int, T> JoinResults = new Dictionary<int, T>();
        public Exception Exception = null;

        private TypeAccessor accessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Dook.EntitySet"/> class.
        /// </summary>
        /// <param name="ConnectionString">Connection string.</param>
        public EntitySet(QueryProvider provider) : base(provider)
        {

        }

        private TypeAccessor GetTypeAccessor()
        {
            if (accessor == null) accessor = TypeAccessor.Create(typeof(T));
            return accessor;
        }


        public void AddFromReader(IDataReader oReader, int position)
        { 
            T entity = ObjectReader.GetEntityUsingIndex<T>(oReader, position, TableMapping, GetTypeAccessor());
            //This if does not allow null entries comming from LEFT or RIGHT joins to be added to DataStore
            if (entity != null && entity.Id > 0)
            {
                if (!JoinResults.ContainsKey(entity.Id))
                {
                    JoinResults.Add(entity.Id, entity);
                }
            }
        }

        public void Clear()
        {
            JoinResults = new Dictionary<int, T>();
        }

        /// <summary>
        /// Updates the specified entity.
        /// </summary>
        /// <returns>Nothing.</returns>
        /// <param name="entity">The updated Entity.</param>
        public void Update(T entity)
        {
            JoinResults[entity.Id] = entity;
            IDbCommand cmd = QueryProvider.GetUpdateCommand(entity, TableName, TableMapping);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Inserts the specified Entity.
        /// </summary>
        /// <returns>Nothing.</returns>
        /// <param name="entity">The inserted Entity.</param>
        public void Insert(T entity)
        {
            IDbCommand cmd = QueryProvider.GetInsertCommand(entity, TableName, TableMapping);
            entity.Id = Convert.ToInt32(cmd.ExecuteScalar());
        }

        /// <summary>
        /// Inserts a collection of Entities.
        /// </summary>
        /// <param name="entities">Entities.</param>
        public void InsertMany(IEnumerable<T> entities)
        {
            foreach (T entity in entities)
            {
                Insert(entity);
            }
        }

        /// <summary>
        /// Deletes an <typeparamref name="Entity"/> based on its id.
        /// </summary>
        /// <returns>Nothing.</returns>
        /// <param name="id">The deleted Entity's id.</param>
        public void Delete(int id)
        {
            IDbCommand cmd = QueryProvider.GetDeleteCommand(id, TableName, TableMapping);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Deletes the specified <typeparamref name="T"/>.
        /// </summary>
        /// <returns>Nothing.</returns>
        /// <param name="entity">The deleted Entity.</param>
        public void Delete(T entity)
        {
            Delete(entity.Id);
        }

        /// <summary>
        /// Deletes a collection of <typeparamref name="T"/>.
        /// </summary>
        /// <param name="entities">Entities.</param>
        public void DeleteMany(IEnumerable<T> entities)
        {
            foreach (T entity in entities)
            {
                Delete(entity);
            }
        }

        /// <summary>
        /// Deletes all entities of type <typeparamref name="T"/>
        /// </summary>
        public void DeleteWhere(Expression<Func<T,bool>> expression)
        {
            IDbCommand cmd = QueryProvider.GetDeleteWhereCommand(expression, TableName);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Deletes all entities of type <typeparamref name="T"/>
        /// </summary>
        public void DeleteAll()
        {
            IDbCommand cmd = QueryProvider.GetDeleteAllCommand(TableName);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Adds the specified Entity to this EntitySet. It also validates that
        /// the added Entity doesn't currently exist in this EntitySet.
        /// </summary>
        /// <returns>Nothing.</returns>
        /// <param name="Entity">The added Entity.</param>
        public void Add(T Entity)
        {
            if (JoinResults.ContainsKey(Entity.Id))
            {
                throw new Exception("An entity with this ID already exists.");
            }
            JoinResults.Add(Entity.Id, Entity);
        }
    }

}

public static class EntitySetExtensions
{
    public static T GetById<T>(this EntitySet<T> entitySet, int Id) where T : class, IEntity, new()
    {
        return entitySet.FirstOrDefault(x => x.Id == Id);
    }
}









