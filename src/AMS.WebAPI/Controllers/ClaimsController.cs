using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AMS.Core.DTOs;
using AMS.Core.Entities;
using AMS.Core.Interfaces;
using AMS.Core.Enums;
using AMS.Infrastructure.Data;

namespace AMS.WebAPI.Controllers;

/// <summary>
/// Claims Controller - Manages insurance claims with document upload support
/// 
/// FLOW WITH AZURE QUEUES:
/// 1. Client creates a claim via POST /api/claims
/// 2. Claim is saved to SQL Database
/// 3. Message is sent to "claim-submitted" queue
/// 4. Azure Function picks up message and sends email notification
/// 5. Client uploads documents via POST /api/claims/{id}/documents
/// 6. Documents are stored in Azure Blob Storage
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClaimsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly AMSDbContext _context;
    private readonly IBlobStorageService? _blobStorage;
    private readonly IQueueService? _queueService;
    private readonly ILogger<ClaimsController> _logger;
    
    // Container name for claim documents in Azure Blob Storage
    private const string DOCUMENTS_CONTAINER = "claim-documents";
    
    public ClaimsController(
        IUnitOfWork unitOfWork,
        AMSDbContext context,
        ILogger<ClaimsController> logger,
        IBlobStorageService? blobStorage = null,
        IQueueService? queueService = null)  // Optional - may not be configured
    {
        _unitOfWork = unitOfWork;
        _context = context;
        _logger = logger;
        _blobStorage = blobStorage;
        _queueService = queueService;
    }
    
    /// <summary>
    /// GET /api/claims
    /// Returns all claims
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClaimDto>>> GetClaims()
    {
        var claims = await _context.Claims
            .Where(c => c.IsActive)
            .Include(c => c.Policy)
            .Include(c => c.Client)
            .Include(c => c.Documents)
            .OrderByDescending(c => c.FiledDate)
            .Select(c => new ClaimDto
            {
                Id = c.Id,
                ClaimNumber = c.ClaimNumber,
                PolicyId = c.PolicyId,
                PolicyNumber = c.Policy != null ? c.Policy.PolicyNumber : "",
                ClientId = c.ClientId,
                ClientName = c.Client != null ? c.Client.FirstName + " " + c.Client.LastName : "",
                Status = c.Status,
                ClaimAmount = c.ClaimAmount,
                ApprovedAmount = c.ApprovedAmount,
                Description = c.Description,
                IncidentDate = c.IncidentDate,
                FiledDate = c.FiledDate,
                ResolvedDate = c.ResolvedDate,
                ReviewerNotes = c.ReviewerNotes,
                Documents = c.Documents.Where(d => d.IsActive).Select(d => new ClaimDocumentDto
                {
                    Id = d.Id,
                    ClaimId = d.ClaimId,
                    FileName = d.FileName,
                    ContentType = d.ContentType,
                    FileSize = d.FileSize,
                    BlobUrl = d.BlobUrl,
                    Description = d.Description,
                    CreatedAt = d.CreatedAt
                }).ToList()
            })
            .ToListAsync();
        
        return Ok(claims);
    }
    
    /// <summary>
    /// GET /api/claims/{id}
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ClaimDto>> GetClaim(Guid id)
    {
        var claim = await _context.Claims
            .Where(c => c.Id == id && c.IsActive)
            .Include(c => c.Policy)
            .Include(c => c.Client)
            .Include(c => c.Documents)
            .Select(c => new ClaimDto
            {
                Id = c.Id,
                ClaimNumber = c.ClaimNumber,
                PolicyId = c.PolicyId,
                PolicyNumber = c.Policy != null ? c.Policy.PolicyNumber : "",
                ClientId = c.ClientId,
                ClientName = c.Client != null ? c.Client.FirstName + " " + c.Client.LastName : "",
                Status = c.Status,
                ClaimAmount = c.ClaimAmount,
                ApprovedAmount = c.ApprovedAmount,
                Description = c.Description,
                IncidentDate = c.IncidentDate,
                FiledDate = c.FiledDate,
                ResolvedDate = c.ResolvedDate,
                ReviewerNotes = c.ReviewerNotes,
                Documents = c.Documents.Where(d => d.IsActive).Select(d => new ClaimDocumentDto
                {
                    Id = d.Id,
                    ClaimId = d.ClaimId,
                    FileName = d.FileName,
                    ContentType = d.ContentType,
                    FileSize = d.FileSize,
                    BlobUrl = d.BlobUrl,
                    Description = d.Description,
                    CreatedAt = d.CreatedAt
                }).ToList()
            })
            .FirstOrDefaultAsync();
        
        if (claim == null)
        {
            return NotFound(new { message = "Claim not found" });
        }
        
        return Ok(claim);
    }
    
    /// <summary>
    /// POST /api/claims
    /// Creates a new claim and sends notification to queue
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ClaimDto>> CreateClaim([FromBody] CreateClaimDto dto)
    {
        // Verify policy exists and is active
        var policy = await _context.Policies
            .Include(p => p.Client)
            .FirstOrDefaultAsync(p => p.Id == dto.PolicyId && p.IsActive);
        
        if (policy == null)
        {
            return BadRequest(new { message = "Policy not found or not active" });
        }
        
        // Generate claim number
        var claimNumber = $"CLM-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}";
        
        var claim = new Claim
        {
            ClaimNumber = claimNumber,
            PolicyId = dto.PolicyId,
            ClientId = policy.ClientId,
            Status = ClaimStatus.Submitted,
            ClaimAmount = dto.ClaimAmount,
            Description = dto.Description,
            IncidentDate = dto.IncidentDate,
            FiledDate = DateTime.UtcNow
        };
        
        await _unitOfWork.Claims.AddAsync(claim);
        await _unitOfWork.SaveChangesAsync();
        
        _logger.LogInformation("Created claim {ClaimNumber} for policy {PolicyNumber}", 
            claimNumber, policy.PolicyNumber);
        
        // Send message to queue for async processing (email notification)
        if (_queueService != null)
        {
            try
            {
                await _queueService.SendClaimSubmittedAsync(new ClaimSubmittedMessage
                {
                    ClaimId = claim.Id,
                    ClaimNumber = claim.ClaimNumber,
                    PolicyId = policy.Id,
                    PolicyNumber = policy.PolicyNumber,
                    ClientId = policy.ClientId,
                    ClientName = policy.Client?.FullName ?? "",
                    ClientEmail = policy.Client?.Email ?? "",
                    ClaimAmount = claim.ClaimAmount,
                    Description = claim.Description,
                    IncidentDate = claim.IncidentDate,
                    FiledDate = claim.FiledDate
                });
                _logger.LogInformation("Claim submitted message sent to queue for {ClaimNumber}", claimNumber);
            }
            catch (Exception ex)
            {
                // Don't fail the request if queue message fails
                _logger.LogWarning(ex, "Failed to send claim submitted message to queue for {ClaimNumber}", claimNumber);
            }
        }
        
        return CreatedAtAction(nameof(GetClaim), new { id = claim.Id }, new ClaimDto
        {
            Id = claim.Id,
            ClaimNumber = claim.ClaimNumber,
            PolicyId = claim.PolicyId,
            PolicyNumber = policy.PolicyNumber,
            ClientId = claim.ClientId,
            ClientName = policy.Client?.FullName ?? "",
            Status = claim.Status,
            ClaimAmount = claim.ClaimAmount,
            Description = claim.Description,
            IncidentDate = claim.IncidentDate,
            FiledDate = claim.FiledDate,
            Documents = new List<ClaimDocumentDto>()
        });
    }
    
    /// <summary>
    /// PUT /api/claims/{id}
    /// Updates claim status and sends notification to queue
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ClaimDto>> UpdateClaim(Guid id, [FromBody] UpdateClaimDto dto)
    {
        var claim = await _context.Claims
            .Include(c => c.Client)
            .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);
            
        if (claim == null)
        {
            return NotFound(new { message = "Claim not found" });
        }
        
        var oldStatus = claim.Status;
        
        if (dto.Status.HasValue)
        {
            claim.Status = dto.Status.Value;
            
            // Set resolved date when claim is approved or rejected
            if (dto.Status == ClaimStatus.Approved || dto.Status == ClaimStatus.Rejected)
            {
                claim.ResolvedDate = DateTime.UtcNow;
            }
        }
        
        if (dto.ApprovedAmount.HasValue) claim.ApprovedAmount = dto.ApprovedAmount.Value;
        if (!string.IsNullOrEmpty(dto.ReviewerNotes)) claim.ReviewerNotes = dto.ReviewerNotes;
        
        claim.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();
        
        _logger.LogInformation("Updated claim {ClaimNumber}: Status {OldStatus} -> {NewStatus}", 
            claim.ClaimNumber, oldStatus, claim.Status);
        
        // Send status change message to queue if status changed
        if (_queueService != null && dto.Status.HasValue && oldStatus != claim.Status)
        {
            try
            {
                await _queueService.SendClaimStatusChangedAsync(new ClaimStatusChangedMessage
                {
                    ClaimId = claim.Id,
                    ClaimNumber = claim.ClaimNumber,
                    ClientName = claim.Client?.FullName ?? "",
                    ClientEmail = claim.Client?.Email ?? "",
                    OldStatus = oldStatus.ToString(),
                    NewStatus = claim.Status.ToString(),
                    ClaimAmount = claim.ClaimAmount,
                    ApprovedAmount = claim.ApprovedAmount,
                    ReviewerNotes = claim.ReviewerNotes,
                    ChangedAt = DateTime.UtcNow
                });
                _logger.LogInformation("Claim status changed message sent to queue for {ClaimNumber}", claim.ClaimNumber);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send claim status changed message to queue for {ClaimNumber}", claim.ClaimNumber);
            }
        }
        
        return Ok(new ClaimDto
        {
            Id = claim.Id,
            ClaimNumber = claim.ClaimNumber,
            PolicyId = claim.PolicyId,
            ClientId = claim.ClientId,
            Status = claim.Status,
            ClaimAmount = claim.ClaimAmount,
            ApprovedAmount = claim.ApprovedAmount,
            Description = claim.Description,
            IncidentDate = claim.IncidentDate,
            FiledDate = claim.FiledDate,
            ResolvedDate = claim.ResolvedDate,
            ReviewerNotes = claim.ReviewerNotes
        });
    }
    
    /// <summary>
    /// POST /api/claims/{id}/documents
    /// Uploads a document to a claim
    /// 
    /// DOCUMENT UPLOAD FLOW:
    /// 1. Receive file via multipart form data
    /// 2. Generate unique blob name
    /// 3. Upload to Azure Blob Storage
    /// 4. Save metadata to database
    /// 5. Return document info
    /// </summary>
    [HttpPost("{id}/documents")]
    public async Task<ActionResult<ClaimDocumentDto>> UploadDocument(
        Guid id, 
        IFormFile file,
        [FromForm] string? description)
    {
        if (_blobStorage == null)
        {
            return BadRequest(new { message = "File storage is not configured. Please configure Azure Storage connection string." });
        }
        
        var claim = await _unitOfWork.Claims.GetByIdAsync(id);
        if (claim == null)
        {
            return NotFound(new { message = "Claim not found" });
        }
        
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file provided" });
        }
        
        // Validate file size (max 10MB)
        const int maxFileSize = 10 * 1024 * 1024;
        if (file.Length > maxFileSize)
        {
            return BadRequest(new { message = "File size exceeds 10MB limit" });
        }
        
        // Validate file type
        var allowedTypes = new[] { "application/pdf", "image/jpeg", "image/png", "image/gif", "application/msword", 
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
        {
            return BadRequest(new { message = "Invalid file type. Allowed: PDF, JPEG, PNG, GIF, DOC, DOCX" });
        }
        
        try
        {
            // Generate unique blob name
            var fileExtension = Path.GetExtension(file.FileName);
            var blobName = $"{claim.ClaimNumber}/{Guid.NewGuid()}{fileExtension}";
            
            // Upload to Azure Blob Storage
            using var stream = file.OpenReadStream();
            var blobUrl = await _blobStorage.UploadAsync(DOCUMENTS_CONTAINER, blobName, stream, file.ContentType);
            
            // Save document metadata to database
            var document = new ClaimDocument
            {
                ClaimId = id,
                FileName = file.FileName,
                ContentType = file.ContentType,
                FileSize = file.Length,
                BlobUrl = blobUrl,
                BlobName = blobName,
                Description = description
            };
            
            await _unitOfWork.ClaimDocuments.AddAsync(document);
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Uploaded document {FileName} for claim {ClaimNumber}", 
                file.FileName, claim.ClaimNumber);
            
            return Ok(new ClaimDocumentDto
            {
                Id = document.Id,
                ClaimId = document.ClaimId,
                FileName = document.FileName,
                ContentType = document.ContentType,
                FileSize = document.FileSize,
                BlobUrl = document.BlobUrl,
                Description = document.Description,
                CreatedAt = document.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document for claim {ClaimId}", id);
            return StatusCode(500, new { message = "Error uploading document" });
        }
    }
    
    /// <summary>
    /// GET /api/claims/{claimId}/documents/{documentId}/download
    /// Gets a temporary download URL for a document
    /// </summary>
    [HttpGet("{claimId}/documents/{documentId}/download")]
    public async Task<ActionResult> DownloadDocument(Guid claimId, Guid documentId)
    {
        if (_blobStorage == null)
        {
            return BadRequest(new { message = "File storage is not configured" });
        }
        
        var document = await _context.ClaimDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId && d.ClaimId == claimId && d.IsActive);
        
        if (document == null)
        {
            return NotFound(new { message = "Document not found" });
        }
        
        try
        {
            // Generate SAS URL valid for 1 hour
            var sasUrl = await _blobStorage.GetSasUrlAsync(DOCUMENTS_CONTAINER, document.BlobName, TimeSpan.FromHours(1));
            return Ok(new { downloadUrl = sasUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating download URL for document {DocumentId}", documentId);
            return StatusCode(500, new { message = "Error generating download URL" });
        }
    }
    
    /// <summary>
    /// DELETE /api/claims/{id}
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteClaim(Guid id)
    {
        var claim = await _unitOfWork.Claims.GetByIdAsync(id);
        if (claim == null)
        {
            return NotFound(new { message = "Claim not found" });
        }
        
        _unitOfWork.Claims.Delete(claim);
        await _unitOfWork.SaveChangesAsync();
        
        return NoContent();
    }
}
