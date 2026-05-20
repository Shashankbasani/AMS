using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AMS.Core.DTOs;
using AMS.Core.Entities;
using AMS.Core.Interfaces;
using AMS.Core.Enums;
using AMS.Infrastructure.Data;
using AMS.Infrastructure.Services;

namespace AMS.WebAPI.Controllers;

/// <summary>
/// Policies Controller - Manages insurance policies
/// 
/// THIS CONTROLLER NOW USES PolicyNumberService to generate policy numbers!
/// 
/// BEFORE (without service):
/// var policyNumber = $"POL-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}";
/// ^ This creates random policy numbers, not sequential
/// 
/// AFTER (with service):
/// var policyNumber = await _policyNumberService.GenerateAsync(dto.Type);
/// ^ This creates sequential policy numbers: AUT2024000001, AUT2024000002, etc.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PoliciesController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly AMSDbContext _context;
    private readonly IPolicyNumberService _policyNumberService;  // NEW!
    private readonly ILogger<PoliciesController> _logger;
    
    public PoliciesController(
        IUnitOfWork unitOfWork,
        AMSDbContext context,
        IPolicyNumberService policyNumberService,  // NEW! Injected from DI container
        ILogger<PoliciesController> logger)
    {
        _unitOfWork = unitOfWork;
        _context = context;
        _policyNumberService = policyNumberService;  // NEW!
        _logger = logger;
    }
    
    /// <summary>
    /// GET /api/policies
    /// Returns all policies with client information
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PolicyDto>>> GetPolicies()
    {
        var policies = await _context.Policies
            .Where(p => p.IsActive)
            .Include(p => p.Client)
            .Include(p => p.Claims)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PolicyDto
            {
                Id = p.Id,
                PolicyNumber = p.PolicyNumber,
                ClientId = p.ClientId,
                ClientName = p.Client != null ? p.Client.FirstName + " " + p.Client.LastName : "",
                Type = p.Type,
                Status = p.Status,
                PremiumAmount = p.PremiumAmount,
                CoverageAmount = p.CoverageAmount,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                Description = p.Description,
                CreatedAt = p.CreatedAt,
                ClaimCount = p.Claims.Count(c => c.IsActive)
            })
            .ToListAsync();
        
        return Ok(policies);
    }
    
    /// <summary>
    /// GET /api/policies/{id}
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<PolicyDto>> GetPolicy(Guid id)
    {
        var policy = await _context.Policies
            .Where(p => p.Id == id && p.IsActive)
            .Include(p => p.Client)
            .Include(p => p.Claims)
            .Select(p => new PolicyDto
            {
                Id = p.Id,
                PolicyNumber = p.PolicyNumber,
                ClientId = p.ClientId,
                ClientName = p.Client != null ? p.Client.FirstName + " " + p.Client.LastName : "",
                Type = p.Type,
                Status = p.Status,
                PremiumAmount = p.PremiumAmount,
                CoverageAmount = p.CoverageAmount,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                Description = p.Description,
                CreatedAt = p.CreatedAt,
                ClaimCount = p.Claims.Count(c => c.IsActive)
            })
            .FirstOrDefaultAsync();
        
        if (policy == null)
        {
            return NotFound(new { message = "Policy not found" });
        }
        
        return Ok(policy);
    }
    
    /// <summary>
    /// POST /api/policies
    /// Creates a new policy
    /// 
    /// NOW USES PolicyNumberService TO GENERATE SEQUENTIAL POLICY NUMBERS!
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PolicyDto>> CreatePolicy([FromBody] CreatePolicyDto dto)
    {
        try
        {
            // Verify client exists
            var client = await _unitOfWork.Clients.GetByIdAsync(dto.ClientId);
            if (client == null)
            {
                return BadRequest(new { message = "Client not found" });
            }
            
            // ===== NEW: USE POLICY NUMBER SERVICE TO GENERATE SEQUENTIAL NUMBER =====
            // This replaces the old random method:
            // OLD: var policyNumber = $"POL-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}";
            var policyNumber = await _policyNumberService.GenerateAsync(dto.Type);
            // ===== END NEW CODE =====
            
            var policy = new Policy
            {
                PolicyNumber = policyNumber,
                ClientId = dto.ClientId,
                Type = dto.Type,
                Status = PolicyStatus.Active,
                PremiumAmount = dto.PremiumAmount,
                CoverageAmount = dto.CoverageAmount,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Description = dto.Description
            };
            
            await _unitOfWork.Policies.AddAsync(policy);
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Created policy {PolicyNumber} for client {ClientId} using PolicyNumberService", 
                policyNumber, dto.ClientId);
            
            return CreatedAtAction(nameof(GetPolicy), new { id = policy.Id }, new PolicyDto
            {
                Id = policy.Id,
                PolicyNumber = policy.PolicyNumber,
                ClientId = policy.ClientId,
                ClientName = client.FirstName + " " + client.LastName,
                Type = policy.Type,
                Status = policy.Status,
                PremiumAmount = policy.PremiumAmount,
                CoverageAmount = policy.CoverageAmount,
                StartDate = policy.StartDate,
                EndDate = policy.EndDate,
                Description = policy.Description,
                CreatedAt = policy.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating policy for client {ClientId}", dto.ClientId);
            return StatusCode(500, new { message = "Error creating policy", error = ex.Message });
        }
    }
    
    /// <summary>
    /// PUT /api/policies/{id}
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<PolicyDto>> UpdatePolicy(Guid id, [FromBody] UpdatePolicyDto dto)
    {
        var policy = await _unitOfWork.Policies.GetByIdAsync(id);
        if (policy == null)
        {
            return NotFound(new { message = "Policy not found" });
        }
        
        if (dto.Status.HasValue) policy.Status = dto.Status.Value;
        if (dto.PremiumAmount.HasValue) policy.PremiumAmount = dto.PremiumAmount.Value;
        if (dto.CoverageAmount.HasValue) policy.CoverageAmount = dto.CoverageAmount.Value;
        if (dto.EndDate.HasValue) policy.EndDate = dto.EndDate.Value;
        if (!string.IsNullOrEmpty(dto.Description)) policy.Description = dto.Description;
        
        _unitOfWork.Policies.Update(policy);
        await _unitOfWork.SaveChangesAsync();
        
        return Ok(new PolicyDto
        {
            Id = policy.Id,
            PolicyNumber = policy.PolicyNumber,
            ClientId = policy.ClientId,
            Type = policy.Type,
            Status = policy.Status,
            PremiumAmount = policy.PremiumAmount,
            CoverageAmount = policy.CoverageAmount,
            StartDate = policy.StartDate,
            EndDate = policy.EndDate,
            Description = policy.Description,
            CreatedAt = policy.CreatedAt
        });
    }
    
    /// <summary>
    /// DELETE /api/policies/{id}
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePolicy(Guid id)
    {
        var policy = await _unitOfWork.Policies.GetByIdAsync(id);
        if (policy == null)
        {
            return NotFound(new { message = "Policy not found" });
        }
        
        _unitOfWork.Policies.Delete(policy);
        await _unitOfWork.SaveChangesAsync();
        
        return NoContent();
    }
}
