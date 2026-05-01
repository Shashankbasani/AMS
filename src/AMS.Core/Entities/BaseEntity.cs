namespace AMS.Core.Entities;

/// <summary>
/// Base class for all entities. Contains common properties.
/// All entities inherit from this class.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Primary key - using Guid for better distributed systems support
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// When the record was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the record was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// Soft delete flag - instead of deleting, we mark as inactive
    /// </summary>
    public bool IsActive { get; set; } = true;
}
