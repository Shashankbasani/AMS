using AMS.Core.Enums;

namespace AMS.Core.Entities;

/// <summary>
/// Represents an insurance policy
/// </summary>
public class Policy : BaseEntity
{
    /// <summary>
    /// Unique policy number (human-readable identifier)
    /// </summary>
    public string PolicyNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// Foreign key to Client
    /// </summary>
    public Guid ClientId { get; set; }
    
    public PolicyType Type { get; set; }
    
    public PolicyStatus Status { get; set; } = PolicyStatus.Pending;
    
    public decimal PremiumAmount { get; set; }
    
    public decimal CoverageAmount { get; set; }
    
    public DateTime StartDate { get; set; }
    
    public DateTime EndDate { get; set; }
    
    public string Description { get; set; } = string.Empty;
    
    // Navigation properties
    public virtual Client? Client { get; set; }
    
    public virtual ICollection<Claim> Claims { get; set; } = new List<Claim>();
}
