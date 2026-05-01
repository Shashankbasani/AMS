namespace AMS.Core.Entities;

/// <summary>
/// Represents a client (customer) who can have policies and claims
/// </summary>
public class Client : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    
    public string LastName { get; set; } = string.Empty;
    
    public string Email { get; set; } = string.Empty;
    
    public string Phone { get; set; } = string.Empty;
    
    public string Address { get; set; } = string.Empty;
    
    public string City { get; set; } = string.Empty;
    
    public string State { get; set; } = string.Empty;
    
    public string ZipCode { get; set; } = string.Empty;
    
    public DateTime DateOfBirth { get; set; }
    
    // Navigation properties - EF Core will understand these relationships
    public virtual ICollection<Policy> Policies { get; set; } = new List<Policy>();
    
    // Helper property
    public string FullName => $"{FirstName} {LastName}";
}
