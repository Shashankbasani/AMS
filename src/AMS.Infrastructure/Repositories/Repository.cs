using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using AMS.Core.Entities;
using AMS.Core.Interfaces;
using AMS.Infrastructure.Data;

namespace AMS.Infrastructure.Repositories;

/// <summary>
/// Generic Repository Implementation
/// 
/// This class implements the IRepository interface for any entity type.
/// It provides CRUD operations using Entity Framework Core.
/// 
/// HOW IT WORKS:
/// 1. Gets the DbContext through constructor injection
/// 2. Gets the DbSet<T> for the specific entity type
/// 3. Uses EF Core methods to query and manipulate data
/// 
/// The repository DOES NOT call SaveChanges() - that's the job of UnitOfWork
/// This allows multiple repository operations to be in one transaction
/// </summary>
public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly AMSDbContext _context;
    protected readonly DbSet<T> _dbSet;
    
    public Repository(AMSDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }
    
    /// <summary>
    /// Get entity by ID
    /// 
    /// SQL Generated:
    /// SELECT * FROM [TableName] WHERE Id = @id AND IsActive = 1
    /// </summary>
    public virtual async Task<T?> GetByIdAsync(Guid id)
    {
        return await _dbSet
            .Where(e => e.IsActive)
            .FirstOrDefaultAsync(e => e.Id == id);
    }
    
    /// <summary>
    /// Get all active entities
    /// 
    /// SQL Generated:
    /// SELECT * FROM [TableName] WHERE IsActive = 1
    /// </summary>
    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet
            .Where(e => e.IsActive)
            .ToListAsync();
    }
    
    /// <summary>
    /// Find entities matching a condition
    /// 
    /// Example usage:
    /// var clients = await repo.FindAsync(c => c.City == "Seattle");
    /// 
    /// SQL Generated:
    /// SELECT * FROM Clients WHERE City = 'Seattle' AND IsActive = 1
    /// </summary>
    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet
            .Where(e => e.IsActive)
            .Where(predicate)
            .ToListAsync();
    }
    
    /// <summary>
    /// Add a new entity
    /// 
    /// IMPORTANT: This only marks the entity for insertion.
    /// The actual INSERT happens when SaveChanges() is called.
    /// 
    /// SQL Generated (on SaveChanges):
    /// INSERT INTO [TableName] (Id, Col1, Col2, ...) VALUES (@id, @col1, @col2, ...)
    /// </summary>
    public virtual async Task<T> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        return entity;
    }
    
    /// <summary>
    /// Update an existing entity
    /// 
    /// EF Core tracks changes automatically, so just updating the object
    /// properties and calling SaveChanges() will generate UPDATE SQL.
    /// 
    /// This method explicitly marks the entity as modified.
    /// 
    /// SQL Generated (on SaveChanges):
    /// UPDATE [TableName] SET Col1 = @col1, Col2 = @col2 WHERE Id = @id
    /// </summary>
    public virtual void Update(T entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.Entry(entity).State = EntityState.Modified;
    }
    
    /// <summary>
    /// Soft delete an entity
    /// 
    /// Instead of actually deleting, we set IsActive = false.
    /// This preserves data for auditing and allows recovery.
    /// 
    /// SQL Generated (on SaveChanges):
    /// UPDATE [TableName] SET IsActive = 0, UpdatedAt = @now WHERE Id = @id
    /// </summary>
    public virtual void Delete(T entity)
    {
        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;
        _context.Entry(entity).State = EntityState.Modified;
    }
    
    /// <summary>
    /// Check if any entity matches a condition
    /// 
    /// SQL Generated:
    /// SELECT CASE WHEN EXISTS (SELECT 1 FROM [TableName] WHERE [condition]) THEN 1 ELSE 0 END
    /// </summary>
    public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet
            .Where(e => e.IsActive)
            .AnyAsync(predicate);
    }
}
