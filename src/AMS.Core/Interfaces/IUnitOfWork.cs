using AMS.Core.Entities;

namespace AMS.Core.Interfaces;

/// <summary>
/// Unit of Work pattern - coordinates multiple repository operations
/// into a single database transaction
/// 
/// Why use Unit of Work?
/// - Ensures all changes are committed or rolled back together
/// - Prevents partial updates when multiple entities are changed
/// - Gives you a single point to commit all changes
/// </summary>
public interface IUnitOfWork : IDisposable
{
    // Repositories for each entity type
    IRepository<User> Users { get; }
    IRepository<Client> Clients { get; }
    IRepository<Policy> Policies { get; }
    IRepository<Claim> Claims { get; }
    IRepository<ClaimDocument> ClaimDocuments { get; }
    
    /// <summary>
    /// Saves all changes made through repositories to the database
    /// This is where the actual SQL INSERT/UPDATE/DELETE happens
    /// </summary>
    /// <returns>Number of rows affected</returns>
    Task<int> SaveChangesAsync();
}
