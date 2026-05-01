namespace AMS.Core.Entities;

/// <summary>
/// Represents a document attached to a claim
/// Documents are stored in Azure Blob Storage
/// This entity stores metadata and the blob URL
/// </summary>
public class ClaimDocument : BaseEntity
{
    /// <summary>
    /// Foreign key to Claim
    /// </summary>
    public Guid ClaimId { get; set; }
    
    /// <summary>
    /// Original filename uploaded by user
    /// </summary>
    public string FileName { get; set; } = string.Empty;
    
    /// <summary>
    /// Content type (mime type) - e.g., "application/pdf", "image/jpeg"
    /// </summary>
    public string ContentType { get; set; } = string.Empty;
    
    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize { get; set; }
    
    /// <summary>
    /// URL to the file in Azure Blob Storage
    /// </summary>
    public string BlobUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// Name of the blob (unique identifier in storage)
    /// </summary>
    public string BlobName { get; set; } = string.Empty;
    
    /// <summary>
    /// Description of what the document is
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Who uploaded this document
    /// </summary>
    public Guid? UploadedByUserId { get; set; }
    
    // Navigation property
    public virtual Claim? Claim { get; set; }
}
