namespace AMS.Core.Entities;

/// <summary>
/// Represents a user in the system (admin, agent, client)
/// </summary>
public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// For simplicity, storing plain text password (NO HASHING as requested)
    /// In production, NEVER do this - always hash passwords!
    /// </summary>
    public string Password { get; set; } = string.Empty;
    
    public string Email { get; set; } = string.Empty;
    
    public string FirstName { get; set; } = string.Empty;
    
    public string LastName { get; set; } = string.Empty;
    
    public string Role { get; set; } = "User"; // Admin, Agent, User
    
    public DateTime? LastLoginAt { get; set; }
    
    // Full name helper property
    public string FullName => $"{FirstName} {LastName}";
}
