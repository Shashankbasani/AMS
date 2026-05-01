using AMS.Core.Entities;
using AMS.Core.Interfaces;
using AMS.Infrastructure.Data;

namespace AMS.Infrastructure.Repositories;

/// <summary>
/// Unit of Work Implementation
/// 
/// WHAT IS UNIT OF WORK?
/// It's a pattern that:
/// 1. Groups multiple database operations into a single transaction
/// 2. Ensures all changes succeed or fail together (atomicity)
/// 3. Provides a single point to commit changes
/// 
/// EXAMPLE SCENARIO:
/// When creating a claim, you might need to:
/// 1. Insert the claim record
/// 2. Insert multiple claim documents
/// 3. Update the policy's claim count
/// 
/// Without Unit of Work, if step 3 fails, you'd have orphaned data.
/// With Unit of Work, ALL changes are rolled back if ANY step fails.
/// 
/// HOW IT WORKS:
/// 1. Each repository uses the SAME DbContext instance
/// 2. All changes are tracked by that DbContext
/// 3. When SaveChangesAsync() is called, ALL tracked changes are saved in one transaction
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly AMSDbContext _context;
    
    // Lazy initialization - repositories are created only when first accessed
    private IRepository<User>? _users;
    private IRepository<Client>? _clients;
    private IRepository<Policy>? _policies;
    private IRepository<Claim>? _claims;
    private IRepository<ClaimDocument>? _claimDocuments;
    
    public UnitOfWork(AMSDbContext context)
    {
        _context = context;
    }
    
    /// <summary>
    /// Lazy-loaded User repository
    /// Created on first access, then reused
    /// </summary>
    public IRepository<User> Users => 
        _users ??= new Repository<User>(_context);
    
    public IRepository<Client> Clients => 
        _clients ??= new Repository<Client>(_context);
    
    public IRepository<Policy> Policies => 
        _policies ??= new Repository<Policy>(_context);
    
    public IRepository<Claim> Claims => 
        _claims ??= new Repository<Claim>(_context);
    
    public IRepository<ClaimDocument> ClaimDocuments => 
        _claimDocuments ??= new Repository<ClaimDocument>(_context);
    
    /// <summary>
    /// Saves all changes made through any repository
    /// 
    /// WHAT HAPPENS INTERNALLY:
    /// 1. EF Core looks at all tracked entities
    /// 2. For each entity, it checks if it's Added, Modified, or Deleted
    /// 3. Generates appropriate SQL (INSERT, UPDATE, DELETE)
    /// 4. Wraps all SQL in a database transaction
    /// 5. Executes the transaction
    /// 6. Returns the number of rows affected
    /// 
    /// If ANY operation fails, the ENTIRE transaction is rolled back.
    /// </summary>
    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
    
    /// <summary>
    /// Dispose the DbContext to release database connections
    /// </summary>
    public void Dispose()
    {
        _context.Dispose();
    }
}
