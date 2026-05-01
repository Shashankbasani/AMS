using Microsoft.EntityFrameworkCore;
using AMS.Core.Entities;

namespace AMS.Infrastructure.Data;

/// <summary>
/// DbContext is the main class for Entity Framework Core
/// 
/// WHAT IT DOES:
/// 1. Represents a session with the database
/// 2. Allows querying and saving data
/// 3. Tracks changes to entities (Change Tracking)
/// 4. Maps C# classes to database tables
/// 
/// HOW IT WORKS:
/// - DbSet<Entity> = Represents a table in the database
/// - When you call SaveChanges(), EF generates SQL and executes it
/// - EF tracks all changes you make to entities and knows what SQL to generate
/// </summary>
public class AMSDbContext : DbContext
{
    /// <summary>
    /// Constructor - receives configuration from dependency injection
    /// The options contain the connection string and other settings
    /// </summary>
    public AMSDbContext(DbContextOptions<AMSDbContext> options) : base(options)
    {
    }
    
    // ===========================================
    // DbSets - Each DbSet represents a table
    // ===========================================
    
    /// <summary>
    /// Users table - for authentication
    /// </summary>
    public DbSet<User> Users => Set<User>();
    
    /// <summary>
    /// Clients table - customers who buy policies
    /// </summary>
    public DbSet<Client> Clients => Set<Client>();
    
    /// <summary>
    /// Policies table - insurance policies
    /// </summary>
    public DbSet<Policy> Policies => Set<Policy>();
    
    /// <summary>
    /// Claims table - insurance claims filed by clients
    /// </summary>
    public DbSet<Claim> Claims => Set<Claim>();
    
    /// <summary>
    /// ClaimDocuments table - files attached to claims
    /// </summary>
    public DbSet<ClaimDocument> ClaimDocuments => Set<ClaimDocument>();
    
    /// <summary>
    /// OnModelCreating is called when EF is building the model
    /// Here we configure:
    /// - Table names
    /// - Relationships between tables
    /// - Indexes
    /// - Seed data
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // =============================================
        // USER CONFIGURATION
        // =============================================
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Password).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(20);
            
            // Create unique index on Username
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });
        
        // =============================================
        // CLIENT CONFIGURATION
        // =============================================
        modelBuilder.Entity<Client>(entity =>
        {
            entity.ToTable("Clients");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Address).HasMaxLength(200);
            entity.Property(e => e.City).HasMaxLength(50);
            entity.Property(e => e.State).HasMaxLength(50);
            entity.Property(e => e.ZipCode).HasMaxLength(10);
            
            entity.HasIndex(e => e.Email).IsUnique();
        });
        
        // =============================================
        // POLICY CONFIGURATION
        // =============================================
        modelBuilder.Entity<Policy>(entity =>
        {
            entity.ToTable("Policies");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PolicyNumber).IsRequired().HasMaxLength(20);
            entity.Property(e => e.PremiumAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CoverageAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Description).HasMaxLength(500);
            
            entity.HasIndex(e => e.PolicyNumber).IsUnique();
            
            // Relationship: Policy belongs to Client (many-to-one)
            entity.HasOne(p => p.Client)
                  .WithMany(c => c.Policies)
                  .HasForeignKey(p => p.ClientId)
                  .OnDelete(DeleteBehavior.Restrict); // Don't cascade delete
        });
        
        // =============================================
        // CLAIM CONFIGURATION
        // =============================================
        modelBuilder.Entity<Claim>(entity =>
        {
            entity.ToTable("Claims");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ClaimNumber).IsRequired().HasMaxLength(20);
            entity.Property(e => e.ClaimAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.ApprovedAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.ReviewerNotes).HasMaxLength(1000);
            
            entity.HasIndex(e => e.ClaimNumber).IsUnique();
            
            // Relationship: Claim belongs to Policy
            entity.HasOne(c => c.Policy)
                  .WithMany(p => p.Claims)
                  .HasForeignKey(c => c.PolicyId)
                  .OnDelete(DeleteBehavior.Restrict);
            
            // Relationship: Claim belongs to Client
            entity.HasOne(c => c.Client)
                  .WithMany()
                  .HasForeignKey(c => c.ClientId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
        
        // =============================================
        // CLAIM DOCUMENT CONFIGURATION
        // =============================================
        modelBuilder.Entity<ClaimDocument>(entity =>
        {
            entity.ToTable("ClaimDocuments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ContentType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.BlobUrl).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.BlobName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(500);
            
            // Relationship: ClaimDocument belongs to Claim
            entity.HasOne(d => d.Claim)
                  .WithMany(c => c.Documents)
                  .HasForeignKey(d => d.ClaimId)
                  .OnDelete(DeleteBehavior.Cascade); // Delete documents when claim is deleted
        });
        
        // =============================================
        // SEED DATA - Initial data for the database
        // This runs during migration to populate tables
        // =============================================
        SeedData(modelBuilder);
    }
    
    /// <summary>
    /// Seed initial data - NO PASSWORD HASHING (as requested)
    /// </summary>
    private void SeedData(ModelBuilder modelBuilder)
    {
        // Fixed GUIDs for consistency
        var adminUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var agentUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        
        var client1Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var client2Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        
        var policy1Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var policy2Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        
        var claim1Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        
        // Seed Users (PLAIN TEXT PASSWORDS - NOT FOR PRODUCTION!)
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = adminUserId,
                Username = "admin",
                Password = "Admin@123",  // Plain text - NOT SECURE!
                Email = "admin@ams.com",
                FirstName = "Admin",
                LastName = "User",
                Role = "Admin",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true
            },
            new User
            {
                Id = agentUserId,
                Username = "agent",
                Password = "Agent@123",  // Plain text - NOT SECURE!
                Email = "agent@ams.com",
                FirstName = "John",
                LastName = "Agent",
                Role = "Agent",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true
            }
        );
        
        // Seed Clients
        modelBuilder.Entity<Client>().HasData(
            new Client
            {
                Id = client1Id,
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@email.com",
                Phone = "555-123-4567",
                Address = "123 Main Street",
                City = "Seattle",
                State = "WA",
                ZipCode = "98101",
                DateOfBirth = new DateTime(1985, 5, 15, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true
            },
            new Client
            {
                Id = client2Id,
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane.smith@email.com",
                Phone = "555-987-6543",
                Address = "456 Oak Avenue",
                City = "Portland",
                State = "OR",
                ZipCode = "97201",
                DateOfBirth = new DateTime(1990, 8, 22, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true
            }
        );
        
        // Seed Policies
        modelBuilder.Entity<Policy>().HasData(
            new Policy
            {
                Id = policy1Id,
                PolicyNumber = "POL-2024-001",
                ClientId = client1Id,
                Type = Core.Enums.PolicyType.Auto,
                Status = Core.Enums.PolicyStatus.Active,
                PremiumAmount = 150.00m,
                CoverageAmount = 50000.00m,
                StartDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Description = "Auto insurance for Toyota Camry 2022",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true
            },
            new Policy
            {
                Id = policy2Id,
                PolicyNumber = "POL-2024-002",
                ClientId = client2Id,
                Type = Core.Enums.PolicyType.Home,
                Status = Core.Enums.PolicyStatus.Active,
                PremiumAmount = 200.00m,
                CoverageAmount = 250000.00m,
                StartDate = new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                Description = "Home insurance for primary residence",
                CreatedAt = new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true
            }
        );
        
        // Seed Claims
        modelBuilder.Entity<Claim>().HasData(
            new Claim
            {
                Id = claim1Id,
                ClaimNumber = "CLM-2024-001",
                PolicyId = policy1Id,
                ClientId = client1Id,
                Status = Core.Enums.ClaimStatus.Submitted,
                ClaimAmount = 5000.00m,
                Description = "Minor fender bender in parking lot",
                IncidentDate = new DateTime(2024, 3, 15, 0, 0, 0, DateTimeKind.Utc),
                FiledDate = new DateTime(2024, 3, 16, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt = new DateTime(2024, 3, 16, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true
            }
        );
    }
}
