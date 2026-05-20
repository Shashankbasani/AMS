using Microsoft.EntityFrameworkCore;
using AMS.Core.Enums;
using AMS.Infrastructure.Data;

namespace AMS.Infrastructure.Services;

/// <summary>
/// Policy Number Generator Service
/// 
/// This service implements the logic of the SQL stored procedure usp_GeneratePolicyNumber in C#.
/// It generates sequential policy numbers based on policy type and year.
/// 
/// EXAMPLE:
/// - PolicyType.Auto (0) = AUT2024000001, AUT2024000002, etc.
/// - PolicyType.Home (1) = HOM2024000001, HOM2024000002, etc.
/// - PolicyType.Life (2) = LIF2024000001, LIF2024000002, etc.
/// 
/// FORMAT: PREFIX + YEAR + 6-DIGIT SEQUENCE
/// - AUT = Auto
/// - HOM = Home
/// - LIF = Life
/// - etc.
/// </summary>
public interface IPolicyNumberService
{
    /// <summary>
    /// Generate the next policy number for the given policy type
    /// </summary>
    Task<string> GenerateAsync(PolicyType policyType);
}

public class PolicyNumberService : IPolicyNumberService
{
    private readonly AMSDbContext _context;
    private readonly ILogger<PolicyNumberService> _logger;
    
    // Mapping of PolicyType enum to prefix (matches SQL stored procedure)
    private static readonly Dictionary<PolicyType, string> TypePrefixMap = new()
    {
        { PolicyType.Auto, "AUT" },
        { PolicyType.Home, "HOM" },
        { PolicyType.Life, "LIF" },
        { PolicyType.Health, "HLT" },
        { PolicyType.Business, "BUS" },
        { PolicyType.Travel, "TRV" },
        { PolicyType.Pet, "PET" },
        { PolicyType.Umbrella, "UMB" },
        { PolicyType.Renters, "RNT" },
        { PolicyType.Other, "OTH" }
    };
    
    public PolicyNumberService(AMSDbContext context, ILogger<PolicyNumberService> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    /// <summary>
    /// Generates the next sequential policy number
    /// 
    /// ALGORITHM:
    /// 1. Get the prefix for the policy type (e.g., "AUT" for Auto)
    /// 2. Get current year (e.g., "2024")
    /// 3. Find the highest existing sequence number for this type+year
    /// 4. Add 1 to it
    /// 5. Pad sequence to 6 digits with leading zeros
    /// 6. Combine: PREFIX + YEAR + SEQUENCE (e.g., "AUT2024000001")
    /// 
    /// THREAD SAFETY:
    /// In a high-concurrency environment, you might want to add database-level locking
    /// or use a separate sequence table. For now, this uses simple LINQ queries.
    /// </summary>
    public async Task<string> GenerateAsync(PolicyType policyType)
    {
        try
        {
            // Get prefix from mapping
            if (!TypePrefixMap.TryGetValue(policyType, out var prefix))
            {
                throw new ArgumentException($"Unknown policy type: {policyType}");
            }
            
            // Get current year
            var currentYear = DateTime.UtcNow.Year.ToString();
            
            // Find all policies with this prefix and year
            // e.g., find all policies starting with "AUT2024"
            var patternStart = prefix + currentYear;
            
            // Get the highest sequence number for this prefix+year combination
            var existingPolicies = await _context.Policies
                .Where(p => p.PolicyNumber.StartsWith(patternStart))
                .Select(p => p.PolicyNumber)
                .ToListAsync();
            
            // Extract sequence numbers and find the maximum
            var maxSequence = 0;
            
            foreach (var policyNumber in existingPolicies)
            {
                // PolicyNumber format: PREFIX + YEAR + 6-digit sequence
                // Total length should be: 3 + 4 + 6 = 13 characters
                if (policyNumber.Length >= 13)
                {
                    var sequenceString = policyNumber.Substring(7, 6); // Get last 6 characters
                    if (int.TryParse(sequenceString, out var sequence))
                    {
                        maxSequence = Math.Max(maxSequence, sequence);
                    }
                }
            }
            
            // Generate next sequence number
            var nextSequence = maxSequence + 1;
            
            // Format: PREFIX + YEAR + Right-padded 6-digit sequence
            var policyNumber = $"{prefix}{currentYear}{nextSequence:D6}";
            
            _logger.LogInformation(
                "Generated policy number {PolicyNumber} for policy type {PolicyType}", 
                policyNumber, policyType);
            
            return policyNumber;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating policy number for policy type {PolicyType}", policyType);
            throw;
        }
    }
}
