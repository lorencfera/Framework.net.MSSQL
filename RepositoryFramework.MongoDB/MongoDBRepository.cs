﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using RepositoryFramework.Interfaces;
using MongoDB.Driver;
using MongoDB.Bson.Serialization;
using System.Reflection;

namespace RepositoryFramework.MongoDB
{
  /// <summary>
  /// Repository that uses the MongoDB document database, see https://docs.mongodb.com/
  /// </summary>
  /// <typeparam name="TEntity"></typeparam>
  public class MongoDBRepository<TEntity> : GenericRepositoryBase<TEntity>, IMongoDBRepository<TEntity>
    where TEntity : class
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="Repository{TEntity}"/> class
    /// <param name="MongoDB database">Database</param>
    /// <param name="classMapInitializer">Class map initializer</param>
    /// </summary>
    public MongoDBRepository(
      IMongoDatabase database,
      Action<BsonClassMap<TEntity>> classMapInitializer)
    {
      if (classMapInitializer != null)
      {
        if (!BsonClassMap.IsClassMapRegistered(typeof(TEntity)))
        {
          try
          {
            BsonClassMap.RegisterClassMap(classMapInitializer);
          }
          catch
          {
          }
        }
      }
      Collection = database.GetCollection<TEntity>($"{EntityTypeName}Collection");
    }

    /// <summary>
    /// MongoDB collection of entities/>
    /// </summary>
    protected virtual IMongoCollection<TEntity> Collection { get; private set; }

    /// <summary>
    /// Gets number of items per page (when paging is used)
    /// </summary>
    public virtual int PageSize { get; private set; } = 0;

    /// <summary>
    /// Gets page number (one based index)
    /// </summary>
    public virtual int PageNumber { get; private set; } = 1;

    /// <summary>
    /// Gets the kind of sort order
    /// </summary>
    public virtual SortOrder SortOrder { get; private set; } = SortOrder.Unspecified;

    /// <summary>
    /// Gets property name for the property to sort by.
    /// </summary>
    public virtual string SortPropertyName { get; private set; } = null;

    /// <summary>
    /// Clear paging
    /// </summary>
    /// <returns>Current instance</returns>
    public IRepository<TEntity> ClearPaging()
    {
      PageSize = 0;
      PageNumber = 1;
      return this;
    }

    /// <summary>
    /// Clear sorting
    /// </summary>
    /// <returns>Current instance</returns>
    public IRepository<TEntity> ClearSorting()
    {
      SortPropertyName = null;
      SortOrder = SortOrder.Unspecified;
      return this;
    }

    /// <summary>
    /// Create a new entity
    /// </summary>
    /// <param name="entity">Entity</param>
    public override void Create(TEntity entity)
    {
      Collection.InsertOne(entity);
    }

    /// <summary>
    /// Create a new entity
    /// </summary>
    /// <param name="entity">Entity</param>
    public override async Task CreateAsync(TEntity entity)
    {
      await Collection.InsertOneAsync(entity);
    }

    /// <summary>
    /// Create a list of new entities
    /// </summary>
    /// <param name="entities">List of entities</param>
    public override void CreateMany(IEnumerable<TEntity> entities)
    {
      Collection.InsertMany(entities);
    }

    /// <summary>
    /// Create a list of new entities
    /// </summary>
    /// <param name="entities">List of entities</param>
    public override async Task CreateManyAsync(IEnumerable<TEntity> entities)
    {
      await Collection.InsertManyAsync(entities);
    }

    /// <summary>
    /// Delete an existing entity
    /// </summary>
    /// <param name="entity">Entity</param>
    public override void Delete(TEntity entity)
    {
      var builder = Builders<TEntity>.Filter;
      var filter = builder.Eq(IdPropertyName, entity.GetType().GetProperty(IdPropertyName).GetValue(entity));
      Collection.DeleteOne(filter);
    }

    /// <summary>
    /// Delete an existing entity
    /// </summary>
    /// <param name="entity">Entity</param>
    public override async Task DeleteAsync(TEntity entity)
    {
      var builder = Builders<TEntity>.Filter;
      var filter = builder.Eq(IdPropertyName, entity.GetType().GetProperty(IdPropertyName).GetValue(entity));
      await Collection.DeleteOneAsync(filter);
    }

    /// <summary>
    /// Delete a list of existing entities
    /// </summary>
    /// <param name="entities">Entity list</param>
    public override void DeleteMany(IEnumerable<TEntity> list)
    {
      var builder = Builders<TEntity>.Filter;
      var filter = builder.In(IdPropertyName, list);
      Collection.DeleteMany(filter);
    }

    /// <summary>
    /// Delete a list of existing entities
    /// </summary>
    /// <param name="entities">Entity list</param>
    public override async Task DeleteManyAsync(IEnumerable<TEntity> list)
    {
      var builder = Builders<TEntity>.Filter;
      var filter = builder.In(IdPropertyName, list);
      await Collection.DeleteManyAsync(filter);
    }

    /// <summary>
    /// Finds a list of items using a filter expression
    /// </summary>
    /// <param name="expr">Filter expression</param>
    /// <returns>List of items</returns>
    public override IEnumerable<TEntity> Find()
    {
      IQueryable<TEntity> query = Collection.AsQueryable();

      return query
        .Sort(this)
        .Page(this)
        .ToList();
    }

    /// <summary>
    /// Finds a list of items using a filter expression
    /// </summary>
    /// <param name="expr">Filter expression</param>
    /// <returns>List of items</returns>
    public override async Task<IEnumerable<TEntity>> FindAsync()
    {
      var find = Collection
        .Find("{}");

      if (SortOrder == SortOrder.Ascending)
      {
        find = find.SortBy(GetPropertySelector(SortPropertyName));
      }
      if (SortOrder == SortOrder.Descending)
      {
        find = find.SortByDescending(GetPropertySelector(SortPropertyName));
      }
      if (PageNumber > 1 || PageSize > 0)
      {
        find = find
          .Skip((PageNumber - 1) * PageSize)
          .Limit(PageSize);
      }
      return await find.ToListAsync();
    }

    /// <summary>
    /// Gets an entity by id.
    /// </summary>
    /// <param name="id">Filter to find a single item</param>
    /// <returns>Entity</returns>
    public override TEntity GetById(object id)
    {
      var builder = Builders<TEntity>.Filter;
      var filter = builder.Eq(IdPropertyName, id);
      return Collection.Find(filter).FirstOrDefault();
    }

    /// <summary>
    /// Gets an entity by id.
    /// </summary>
    /// <param name="id">Filter to find a single item</param>
    /// <returns>Entity</returns>
    public override async Task<TEntity> GetByIdAsync(object id)
    {
      var builder = Builders<TEntity>.Filter;
      var filter = builder.Eq(IdPropertyName, id);
      return await Collection.Find(filter).FirstOrDefaultAsync();
    }

    /// <summary>
    /// Use paging
    /// </summary>
    /// <param name="pageNumber">Page to get (one based index).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <returns>Current instance</returns>
    public IRepository<TEntity> Page(int pageNumber, int pageSize)
    {
      PageSize = pageSize;
      PageNumber = pageNumber;
      return this;
    }

    /// <summary>
    /// Property to sort by (ascending)
    /// </summary>
    /// <param name="property">The property.</param>
    /// <returns>Current instance</returns>
    public IRepository<TEntity> SortBy(Expression<Func<TEntity, object>> property)
    {
      if (property == null)
      {
        throw new ArgumentNullException(nameof(property));
      }

      var name = GetPropertyName(property);
      SortBy(name);
      return this;
    }

    /// <summary>
    /// Sort ascending by a property
    /// </summary>
    /// <param name="propertyName">Name of the property.</param>
    /// <returns>Current instance</returns>
    public IRepository<TEntity> SortBy(string propertyName)
    {
      if (propertyName == null)
      {
        throw new ArgumentNullException(nameof(propertyName));
      }

      ValidatePropertyName(propertyName, out propertyName);

      SortOrder = SortOrder.Ascending;
      SortPropertyName = propertyName;
      return this;
    }

    /// <summary>
    /// Property to sort by (descending)
    /// </summary>
    /// <param name="property">The property</param>
    /// <returns>Current instance</returns>
    public IRepository<TEntity> SortByDescending(Expression<Func<TEntity, object>> property)
    {
      if (property == null)
      {
        throw new ArgumentNullException(nameof(property));
      }

      var name = GetPropertyName(property);
      SortByDescending(name);
      return this;
    }

    /// <summary>
    /// Sort descending by a property.
    /// </summary>
    /// <param name="propertyName">Name of the property.</param>
    /// <returns>Current instance</returns>
    public IRepository<TEntity> SortByDescending(string propertyName)
    {
      if (propertyName == null)
      {
        throw new ArgumentNullException(nameof(propertyName));
      }

      ValidatePropertyName(propertyName, out propertyName);

      SortOrder = SortOrder.Descending;
      SortPropertyName = propertyName;
      return this;
    }

    /// <summary>
    /// Update an existing entity
    /// </summary>
    /// <param name="entity">Entity</param>
    public override void Update(TEntity entity)
    {
      var builder = Builders<TEntity>.Filter;
      var filter = builder.Eq(IdPropertyName, entity.GetType().GetProperty(IdPropertyName).GetValue(entity));
      Collection.ReplaceOne(filter, entity);
    }

    /// <summary>
    /// Update an existing entity
    /// </summary>
    /// <param name="entity">Entity</param>
    public override async Task UpdateAsync(TEntity entity)
    {
      var builder = Builders<TEntity>.Filter;
      var filter = builder.Eq(IdPropertyName, entity.GetType().GetProperty(IdPropertyName).GetValue(entity));
      await Collection.ReplaceOneAsync(filter, entity);
    }

    /// <summary>
    /// Gets a queryable collection of entities
    /// </summary>
    /// <returns>Queryable collection of entities</returns>
    public IQueryable<TEntity> AsQueryable()
    {
      IQueryable<TEntity> query = Collection.AsQueryable();

      return query
        .Sort(this)
        .Page(this);
    }
  }
}
