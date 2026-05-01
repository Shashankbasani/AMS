using AMS.Core.Enums;

namespace AMS.Core.Entities;

/// <summary>
/// Represents an insurance claim filed by a client
/// </summary>
public class Claim : BaseEntity
{
    /// <summary>
    /// Unique claim number (human-readable identifier)
    /// </summary>
    public string ClaimNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// Foreign key to Policy
    /// </summary>
    public Guid PolicyId { get; set; }
    
    /// <summary>
    /// Foreign key to Client
    /// </summary>
    public Guid ClientId { get; set; }
    
    public ClaimStatus Status { get; set; } = ClaimStatus.Submitted;
    
    /// <summary>
    /// Amount being claimed
    /// </summary>
    public decimal ClaimAmount { get; set; }
    
    /// <summary>
    /// Amount approved (can be different from claimed)
    /// </summary>
    public decimal? ApprovedAmount { get; set; }
    
    /// <summary>
    /// Description of what happened
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Date when the incident occurred
    /// </summary>
    public DateTime IncidentDate { get; set; }
    
    /// <summary>
    /// Date when claim was filed
    /// </summary>
    public DateTime FiledDate { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Date when claim was resolved (approved/rejected)
    /// </summary>
    public DateTime? ResolvedDate { get; set; }
    
    /// <summary>
    /// Notes from the reviewer
    /// </summary>
    public string? ReviewerNotes { get; set; }
    
    // Navigation properties
    public virtual Policy? Policy { get; set; }
    
    public virtual Client? Client { get; set; }
    
    /// <summary>
    /// Documents attached to this claim (stored in Azure Blob Storage)
    /// </summary>
    public virtual ICollection<ClaimDocument> Documents { get; set; } = new List<ClaimDocument>();
}
