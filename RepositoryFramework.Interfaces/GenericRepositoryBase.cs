﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace RepositoryFramework.Interfaces
{
  /// <summary>
  /// Base class for generic repositories
  /// </summary>
  /// <typeparam name="TEntity">Entoty type</typeparam>
  public abstract class GenericRepositoryBase<TEntity> :
    IRepository<TEntity>
    where TEntity : class
  {
    /// <summary>
    /// Sets entity id property
    /// </summary>
    /// <param name="idProperty">Property expression</param>
    public static void SetIdProperty(Expression<Func<TEntity, object>> idProperty = null)
    {
      if (idProperty == null)
      {
        IdPropertyName = FindIdProperty();
      }
      else
      {
        IdPropertyName = GetPropertyName(idProperty);
      }
    }

    /// <summary>
    /// Gets entity type
    /// </summary>
    protected static Type EntityType { get; private set; } = typeof(TEntity);

    /// <summary>
    /// Gets entity type name
    /// </summary>
    protected static string EntityTypeName { get; private set; } = typeof(TEntity).Name;

    /// <summary>
    /// Gets entity database columns: all value type properties
    /// </summary>
    protected static string[] EntityColumns { get; private set; } = typeof(TEntity).GetProperties()
      .Where(p => (p.PropertyType.GetTypeInfo().GetInterface("IEnumerable") == null
      && p.PropertyType.GetTypeInfo().GetInterface("ICollection") == null
      && !p.PropertyType.GetTypeInfo().IsClass)
      || p.PropertyType.IsAssignableFrom(typeof(string)))
      .Select(p => p.Name)
      .ToArray();

    /// <summary>
    /// Gets or sets entity Id property
    /// </summary>
    protected static string IdPropertyName { get; set; } = FindIdProperty();

    /// <summary>
    /// Create a new entity
    /// </summary>
    /// <param name="entity">Entity</param>
    public virtual void Create(TEntity entity)
    {
      CreateAsync(entity).WaitSync();
    }

    /// <summary>
    /// Create a new entity
    /// </summary>
    /// <param name="entity">Entity</param>
    public virtual async Task CreateAsync(TEntity entity)
    {
      var task = Task.Run(() => Create(entity));
      await task;
    }

    /// <summary>
    /// Create a list of new entities
    /// </summary>
    /// <param name="entities">List of entities</param>
    public virtual void CreateMany(IEnumerable<TEntity> entities)
    {
      CreateManyAsync(entities).WaitSync();
    }

    /// <summary>
    /// Create a list of new entities
    /// </summary>
    /// <param name="entities">List of entities</param>
    public virtual async Task CreateManyAsync(IEnumerable<TEntity> entities)
    {
      var task = Task.Run(() => CreateMany(entities));
      await task;
    }

    /// <summary>
    /// Delete an existing entity
    /// </summary>
    /// <param name="entity">Entity</param>
    public virtual void Delete(TEntity entity)
    {
      DeleteAsync(entity).WaitSync();
    }

    /// <summary>
    /// Delete an existing entity
    /// </summary>
    /// <param name="entity">Entity</param>
    public virtual async Task DeleteAsync(TEntity entity)
    {
      var task = Task.Run(() => Delete(entity));
      await task;
    }

    /// <summary>
    /// Delete a list of existing entities
    /// </summary>
    /// <param name="entities">Entity list</param>
    public virtual void DeleteMany(IEnumerable<TEntity> entities)
    {
      DeleteManyAsync(entities).WaitSync();
    }

    /// <summary>
    /// Delete a list of existing entities
    /// </summary>
    /// <param name="entities">Entity list</param>
    public virtual async Task DeleteManyAsync(IEnumerable<TEntity> entities)
    {
      var task = Task.Run(() => DeleteMany(entities));
      await task;
    }

    /// <summary>
    /// Get a list of entities
    /// </summary>
    /// <returns>Query result</returns>
    public virtual IEnumerable<TEntity> Find()
    {
      var task = FindAsync();
      task.WaitSync();
      return task.Result;
    }

    /// <summary>
    /// Get a list of entities
    /// </summary>
    /// <returns>Query result</returns>
    public virtual async Task<IEnumerable<TEntity>> FindAsync()
    {
      var task = Task<IEnumerable<TEntity>>.Run(() => Find());
      return await task;
    }

    /// <summary>
    /// Gets an entity by id.
    /// </summary>
    /// <param name="id">Filter</param>
    /// <returns>Entity</returns>
    public virtual TEntity GetById(object id)
    {
      var task = GetByIdAsync(id);
      task.WaitSync();
      return task.Result;
    }

    /// <summary>
    /// Gets an entity by id.
    /// </summary>
    /// <param name="id">Filter</param>
    /// <returns>Entity</returns>
    public virtual async Task<TEntity> GetByIdAsync(object id)
    {
      var task = Task<TEntity>.Run(() => GetById(id));
      return await task;
    }

    /// <summary>
    /// Update an existing entity
    /// </summary>
    /// <param name="entity">Entity</param>
    public virtual void Update(TEntity entity)
    {
      UpdateAsync(entity).WaitSync();
    }

    /// <summary>
    /// Update an existing entity
    /// </summary>
    /// <param name="entity">Entity</param>
    public virtual async Task UpdateAsync(TEntity entity)
    {
      var task = Task.Run(() => Update(entity));
      await task;
    }

    /// <summary>
    /// Find the Id property of the entity type looking for properties with name Id or (entity type name)Id
    /// </summary>
    /// <returns>Id property name or null if none could befound</returns>
    protected static string FindIdProperty()
    {
      var idProperty = EntityColumns
        .FirstOrDefault(c => c.ToLower() == $"{EntityTypeName.ToLower()}id");
      if (idProperty == null)
      {
        idProperty = EntityColumns
          .FirstOrDefault(c => c.ToLower() == "id");
      }

      if (idProperty == null)
      {
        return null;
      }

      return idProperty;
    }

    /// <summary>
    /// Get the name of a property from an expression
    /// </summary>
    /// <param name="propertyExpression">Property expression</param>
    /// <returns>Property name</returns>
    protected static string GetPropertyName(Expression<Func<TEntity, object>> propertyExpression)
    {
      var body = propertyExpression.Body as MemberExpression;

      if (body != null)
      {
        return body.Member.Name;
      }

      var ubody = (UnaryExpression)propertyExpression.Body;
      body = ubody.Operand as MemberExpression;

      return body?.Member.Name ?? string.Empty;
    }

    /// <summary>
    /// Chech property name
    /// </summary>
    /// <param name="property">Property name</param>
    /// <param name="validatedPropertyName">Validated property name, casing is corrected</param>
    /// <returns>Success</returns>
    protected static bool TryCheckPropertyName(string property, out string validatedPropertyName)
    {
      validatedPropertyName = property;
      var pi = EntityType.GetProperty(property, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
      if (pi == null)
      {
        return false;
      }

      validatedPropertyName = pi.Name;
      return true;
    }

    /// <summary>
    /// Check property path
    /// </summary>
    /// <param name="path">Path to a property or a property of a related type</param>
    /// <param name="validatedPath">Validated path, property name casing is corrected</param>
    /// <returns>Success</returns>
    protected static bool TryCheckPropertyPath(string path, out string validatedPath)
    {
      validatedPath = path;
      var properties = path.Split('.');
      List<string> validated = new List<string>();

      var type = EntityType;
      foreach (var property in properties)
      {
        var pi = type.GetProperty(property, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
        if (pi == null)
        {
          return false;
        }

        validated.Add(pi.Name);
        if (pi.PropertyType.IsArray)
        {
          type = pi.PropertyType.GetElementType();
        }
        else if (pi.PropertyType.IsConstructedGenericType)
        {
          type = pi.PropertyType.GetGenericArguments().Single();
        }
        else
        {
          type = pi.PropertyType;
        }
      }

      validatedPath = string.Join(".", validated);
      return true;
    }

    /// <summary>
    /// Make sure that the property exists in the model.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="validatedName">Validated name</param>
    protected static void ValidatePropertyName(string name, out string validatedName)
    {
      validatedName = name;
      if (name == null)
      {
        throw new ArgumentNullException(nameof(name));
      }

      if (!TryCheckPropertyName(name, out validatedName))
      {
        throw new ArgumentException(
          string.Format(
            "'{0}' is not a public property of '{1}'.",
            name,
            EntityTypeName));
      }
    }

    /// <summary>
    /// Convert parameter collection to an object
    /// </summary>
    /// <returns>Object</returns>
    protected static object ToObject(IDictionary<string, Object> parameters)
    {
      var dynamicObject = new ExpandoObject() as IDictionary<string, Object>;
      foreach (var parameter in parameters)
      {
        dynamicObject.Add(parameter.Key, parameter.Value);
      }
      return dynamicObject;
    }

    /// <summary>
    /// Gets a property selector expression from the property name
    /// </summary>
    /// <param name="propertyName">Property name</param>
    /// <returns>Property selector expression </returns>
    public static Expression<Func<TEntity, object>> GetPropertySelector(string propertyName)
    {
      var arg = Expression.Parameter(typeof(TEntity), "x");
      var property = Expression.Property(arg, propertyName);
      //return the property as object
      var conv = Expression.Convert(property, typeof(object));
      var exp = Expression.Lambda<Func<TEntity, object>>(conv, new ParameterExpression[] { arg });
      return exp;
    }
  }
}