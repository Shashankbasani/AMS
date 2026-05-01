using AMS.Core.Enums;

namespace AMS.Core.DTOs;

// ===========================================
// POLICY DTOs
// ===========================================

public class PolicyDto
{
    public Guid Id { get; set; }
    public string PolicyNumber { get; set; } = string.Empty;
    public Guid ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public PolicyType Type { get; set; }
    public string TypeName => Type.ToString();
    public PolicyStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public decimal PremiumAmount { get; set; }
    public decimal CoverageAmount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int ClaimCount { get; set; }
}

public class CreatePolicyDto
{
    public Guid ClientId { get; set; }
    public PolicyType Type { get; set; }
    public decimal PremiumAmount { get; set; }
    public decimal CoverageAmount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class UpdatePolicyDto
{
    public PolicyStatus? Status { get; set; }
    public decimal? PremiumAmount { get; set; }
    public decimal? CoverageAmount { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Description { get; set; }
}
