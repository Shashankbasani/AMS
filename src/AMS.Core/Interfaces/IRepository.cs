using System.Linq.Expressions;
using AMS.Core.Entities;

namespace AMS.Core.Interfaces;

/// <summary>
/// Generic repository interface - defines CRUD operations for any entity
/// This is the Repository Pattern - it abstracts data access logic
/// </summary>
/// <typeparam name="T">The entity type (must inherit from BaseEntity)</typeparam>
public interface IRepository<T> where T : BaseEntity
{
    /// <summary>
    /// Get an entity by its ID
    /// </summary>
    Task<T?> GetByIdAsync(Guid id);
    
    /// <summary>
    /// Get all entities
    /// </summary>
    Task<IEnumerable<T>> GetAllAsync();
    
    /// <summary>
    /// Find entities matching a condition
    /// </summary>
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    
    /// <summary>
    /// Add a new entity
    /// </summary>
    Task<T> AddAsync(T entity);
    
    /// <summary>
    /// Update an existing entity
    /// </summary>
    void Update(T entity);
    
    /// <summary>
    /// Delete an entity (soft delete - sets IsActive to false)
    /// </summary>
    void Delete(T entity);
    
    /// <summary>
    /// Check if any entity matches a condition
    /// </summary>
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
}
