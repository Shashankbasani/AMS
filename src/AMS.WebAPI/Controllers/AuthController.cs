using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using AMS.Core.DTOs;
using AMS.Core.Interfaces;

namespace AMS.WebAPI.Controllers;

/*
╔══════════════════════════════════════════════════════════════════════════════╗
║                           CONTROLLER EXPLAINED                                ║
╠══════════════════════════════════════════════════════════════════════════════╣
║                                                                               ║
║  A Controller handles HTTP requests and returns HTTP responses.               ║
║                                                                               ║
║  Key Concepts:                                                                ║
║  - [ApiController] = Enables automatic model validation, binding, etc.        ║
║  - [Route] = Base URL path for all actions in this controller                 ║
║  - [HttpGet/Post/Put/Delete] = HTTP method + route template                   ║
║  - IActionResult = Return type for HTTP responses                             ║
║                                                                               ║
║  Flow:                                                                        ║
║  HTTP Request → Middleware → Controller Action → Repository → Database        ║
║  Database → Repository → Controller Action → HTTP Response                    ║
║                                                                               ║
╚══════════════════════════════════════════════════════════════════════════════╝
*/

[ApiController]
[Route("api/[controller]")]  // Route = /api/auth
public class AuthController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;
    
    /// <summary>
    /// Constructor - Dependencies are INJECTED by the DI container
    /// We don't create these objects ourselves - ASP.NET Core provides them
    /// </summary>
    public AuthController(
        IUnitOfWork unitOfWork, 
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _logger = logger;
    }
    
    /// <summary>
    /// POST /api/auth/login
    /// Authenticates a user and returns a JWT token
    /// 
    /// THE COMPLETE FLOW:
    /// 1. Client sends { username, password } as JSON
    /// 2. ASP.NET Core deserializes JSON into LoginRequestDto
    /// 3. We query the database for the user
    /// 4. Compare passwords (plain text - NO HASHING as requested!)
    /// 5. Generate JWT token
    /// 6. Return token to client
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        _logger.LogInformation("Login attempt for user: {Username}", request.Username);
        
        // ─────────────────────────────────────────────────────────────────────
        // STEP 1: Find user in database
        // This calls Repository → DbContext → SQL Server
        // Generated SQL: SELECT * FROM Users WHERE Username = @username AND IsActive = 1
        // ─────────────────────────────────────────────────────────────────────
        var users = await _unitOfWork.Users.FindAsync(u => u.Username == request.Username);
        var user = users.FirstOrDefault();
        
        if (user == null)
        {
            _logger.LogWarning("User not found: {Username}", request.Username);
            return Unauthorized(new { message = "Invalid username or password" });
        }
        
        // ─────────────────────────────────────────────────────────────────────
        // STEP 2: Verify password (PLAIN TEXT - NOT SECURE!)
        // In production, you'd compare hashed passwords
        // ─────────────────────────────────────────────────────────────────────
        if (user.Password != request.Password)
        {
            _logger.LogWarning("Invalid password for user: {Username}", request.Username);
            return Unauthorized(new { message = "Invalid username or password" });
        }
        
        // ─────────────────────────────────────────────────────────────────────
        // STEP 3: Update last login time
        // ─────────────────────────────────────────────────────────────────────
        user.LastLoginAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();
        
        // ─────────────────────────────────────────────────────────────────────
        // STEP 4: Generate JWT Token
        // ─────────────────────────────────────────────────────────────────────
        var token = GenerateJwtToken(user.Id, user.Username, user.Role);
        
        _logger.LogInformation("User logged in successfully: {Username}", request.Username);
        
        return Ok(new LoginResponseDto
        {
            Token = token,
            Username = user.Username,
            Role = user.Role,
            ExpiresAt = DateTime.UtcNow.AddMinutes(
                int.Parse(_configuration["JwtSettings:ExpirationInMinutes"] ?? "60"))
        });
    }
    
    /// <summary>
    /// Generates a JWT (JSON Web Token)
    /// 
    /// JWT STRUCTURE:
    /// Header.Payload.Signature
    /// 
    /// - Header: Algorithm and token type
    /// - Payload: Claims (user data)
    /// - Signature: Verification that token wasn't tampered with
    /// </summary>
    private string GenerateJwtToken(Guid userId, string username, string role)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        // Claims are pieces of information about the user
        // These are encoded in the token and can be read by the server
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, username),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        
        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"] ?? "AMS",
            audience: jwtSettings["Audience"] ?? "AMS",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(int.Parse(jwtSettings["ExpirationInMinutes"] ?? "60")),
            signingCredentials: credentials
        );
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
