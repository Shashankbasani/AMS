-- Update admin password to 'Admin@123'
-- BCrypt hash for 'Admin@123' generated using BCrypt.Net
UPDATE Users 
SET PasswordHash = '$2a$11$rBLRYFGqAErdVd8xmYvNOeK0RqXrL8V8TvvpqY7CJwKZC9XRXSXTG',
    Email = 'shashankbas315@gmail.com'
WHERE Username = 'admin';

SELECT Id, Username, Email, LEFT(PasswordHash, 50) as PasswordHashPrefix FROM Users;
