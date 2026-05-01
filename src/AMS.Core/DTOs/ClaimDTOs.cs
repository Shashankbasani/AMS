using AMS.Core.Enums;

namespace AMS.Core.DTOs;

// ===========================================
// CLAIM DTOs
// ===========================================

public class ClaimDto
{
    public Guid Id { get; set; }
    public string ClaimNumber { get; set; } = string.Empty;
    public Guid PolicyId { get; set; }
    public string PolicyNumber { get; set; } = string.Empty;
    public Guid ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public ClaimStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public decimal ClaimAmount { get; set; }
    public decimal? ApprovedAmount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime IncidentDate { get; set; }
    public DateTime FiledDate { get; set; }
    public DateTime? ResolvedDate { get; set; }
    public string? ReviewerNotes { get; set; }
    public List<ClaimDocumentDto> Documents { get; set; } = new();
}

public class CreateClaimDto
{
    public Guid PolicyId { get; set; }
    public decimal ClaimAmount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime IncidentDate { get; set; }
}

public class UpdateClaimDto
{
    public ClaimStatus? Status { get; set; }
    public decimal? ApprovedAmount { get; set; }
    public string? ReviewerNotes { get; set; }
}

// ===========================================
// CLAIM DOCUMENT DTOs
// ===========================================

public class ClaimDocumentDto
{
    public Guid Id { get; set; }
    public Guid ClaimId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string BlobUrl { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UploadDocumentDto
{
    public Guid ClaimId { get; set; }
    public string? Description { get; set; }
    // The actual file will be sent as IFormFile in the controller
}
