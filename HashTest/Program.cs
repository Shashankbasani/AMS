// Run with: dotnet run
using BCrypt.Net;

Console.WriteLine("Generating BCrypt hash for 'Admin@123'...");
var hash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
Console.WriteLine($"Hash: {hash}");
Console.WriteLine($"Hash Length: {hash.Length}");

// Verify it works
var verified = BCrypt.Net.BCrypt.Verify("Admin@123", hash);
Console.WriteLine($"Verification: {verified}");

// Also verify with the existing hash from database
var existingHash = "$2a$11$rBLRYFGqAErdVd8xmYvNOeK0RqXrL8V8TvvpqY7CJwKZC9XRXSXTG";
var existingVerified = BCrypt.Net.BCrypt.Verify("Admin@123", existingHash);
Console.WriteLine($"Existing hash verification with 'Admin@123': {existingVerified}");
