-- ============================================
-- Seed Data for Agency Management System
-- ============================================

-- ============================================
-- Seed Roles
-- ============================================
INSERT INTO [dbo].[Role] ([Id], [Name], [Description], [Permissions], [IsActive], [CreatedBy], [ModifiedBy])
VALUES 
(NEWID(), 'Admin', 'System Administrator with full access', 
 '["clients:read","clients:write","clients:delete","policies:read","policies:write","policies:delete","claims:read","claims:write","claims:delete","users:read","users:write","users:delete","reports:read","settings:write"]', 
 1, 'System', 'System'),

(NEWID(), 'Agent', 'Insurance Agent - can manage clients and policies', 
 '["clients:read","clients:write","policies:read","policies:write","claims:read","claims:write","reports:read"]', 
 1, 'System', 'System'),

(NEWID(), 'Underwriter', 'Underwriter - can review and approve policies', 
 '["clients:read","policies:read","policies:write","claims:read","reports:read"]', 
 1, 'System', 'System'),

(NEWID(), 'Adjuster', 'Claims Adjuster - manages claim investigations', 
 '["clients:read","policies:read","claims:read","claims:write","reports:read"]', 
 1, 'System', 'System'),

(NEWID(), 'Viewer', 'Read-only access to the system', 
 '["clients:read","policies:read","claims:read","reports:read"]', 
 1, 'System', 'System');

GO

-- ============================================
-- Seed Admin User (Password: Admin@123)
-- Note: In production, use proper password hashing
-- ============================================
DECLARE @AdminRoleId UNIQUEIDENTIFIER;
SELECT @AdminRoleId = Id FROM [dbo].[Role] WHERE Name = 'Admin';

INSERT INTO [dbo].[User] (
    [Id], [Username], [Email], [PasswordHash], [PasswordSalt],
    [FirstName], [LastName], [RoleId], [Department], [JobTitle],
    [IsActive], [IsEmailVerified], [CreatedBy], [ModifiedBy]
)
VALUES (
    NEWID(), 'admin', 'admin@ams.com',
    -- These are placeholder hashes - replace with proper hashing in production
    'AQAAAAEAACcQAAAAEK2H7k8P1bSP0Pj5qR4T5W6V8X9Y0A1B2C3D4E5F6G7H8I9J0K1L2M3N4O5P6Q7R8S9T0',
    'RandomSaltValue123456789',
    'System', 'Administrator', @AdminRoleId, 'IT', 'System Administrator',
    1, 1, 'System', 'System'
);

GO

-- ============================================
-- Seed Sample Clients (for development/testing)
-- ============================================
INSERT INTO [dbo].[Client] (
    [Id], [FirstName], [LastName], [Email], [PhoneNumber], [Address], 
    [City], [State], [ZipCode], [Country], [DateOfBirth], 
    [Occupation], [Employer], [AnnualIncome], [IsActive], [CreatedBy], [ModifiedBy]
)
VALUES 
(NEWID(), 'John', 'Smith', 'john.smith@email.com', '555-0101', '123 Main Street',
 'Seattle', 'WA', '98101', 'USA', '1985-03-15',
 'Software Engineer', 'Tech Corp', 125000.00, 1, 'System', 'System'),

(NEWID(), 'Sarah', 'Johnson', 'sarah.johnson@email.com', '555-0102', '456 Oak Avenue',
 'Portland', 'OR', '97201', 'USA', '1990-07-22',
 'Marketing Manager', 'Brand Inc', 95000.00, 1, 'System', 'System'),

(NEWID(), 'Michael', 'Williams', 'michael.w@email.com', '555-0103', '789 Pine Road',
 'San Francisco', 'CA', '94102', 'USA', '1978-11-08',
 'Financial Analyst', 'Finance LLC', 110000.00, 1, 'System', 'System'),

(NEWID(), 'Emily', 'Brown', 'emily.brown@email.com', '555-0104', '321 Elm Street',
 'Los Angeles', 'CA', '90001', 'USA', '1992-01-30',
 'Teacher', 'City Schools', 65000.00, 1, 'System', 'System'),

(NEWID(), 'David', 'Davis', 'david.davis@email.com', '555-0105', '654 Maple Drive',
 'Denver', 'CO', '80201', 'USA', '1983-05-17',
 'Architect', 'Design Studio', 145000.00, 1, 'System', 'System');

GO

-- ============================================
-- Seed Sample Policies
-- ============================================
DECLARE @Client1 UNIQUEIDENTIFIER, @Client2 UNIQUEIDENTIFIER, @Client3 UNIQUEIDENTIFIER;

SELECT TOP 1 @Client1 = Id FROM [dbo].[Client] WHERE Email = 'john.smith@email.com';
SELECT TOP 1 @Client2 = Id FROM [dbo].[Client] WHERE Email = 'sarah.johnson@email.com';
SELECT TOP 1 @Client3 = Id FROM [dbo].[Client] WHERE Email = 'michael.w@email.com';

-- Auto policies
INSERT INTO [dbo].[Policy] (
    [Id], [PolicyNumber], [ClientId], [PolicyType], [Status], 
    [EffectiveDate], [ExpirationDate], [PremiumAmount], [DeductibleAmount], [CoverageLimit],
    [Description], [PaymentFrequency], [CreatedBy], [ModifiedBy]
)
VALUES 
(NEWID(), 'AUT2024000001', @Client1, 0, 1, 
 '2024-01-01', '2025-01-01', 1200.00, 500.00, 100000.00,
 'Full coverage auto insurance for 2022 Honda Accord', 'Monthly', 'System', 'System'),

(NEWID(), 'AUT2024000002', @Client2, 0, 1, 
 '2024-02-15', '2025-02-15', 950.00, 750.00, 75000.00,
 'Liability coverage for 2020 Toyota Camry', 'Quarterly', 'System', 'System');

-- Home policies
INSERT INTO [dbo].[Policy] (
    [Id], [PolicyNumber], [ClientId], [PolicyType], [Status], 
    [EffectiveDate], [ExpirationDate], [PremiumAmount], [DeductibleAmount], [CoverageLimit],
    [Description], [PaymentFrequency], [CreatedBy], [ModifiedBy]
)
VALUES 
(NEWID(), 'HOM2024000001', @Client1, 1, 1, 
 '2024-01-01', '2025-01-01', 2400.00, 2500.00, 500000.00,
 'Homeowners insurance for 123 Main Street property', 'SemiAnnually', 'System', 'System'),

(NEWID(), 'HOM2024000002', @Client3, 1, 1, 
 '2024-03-01', '2025-03-01', 3200.00, 5000.00, 750000.00,
 'Premium homeowners coverage with flood protection', 'Annually', 'System', 'System');

-- Life policy
INSERT INTO [dbo].[Policy] (
    [Id], [PolicyNumber], [ClientId], [PolicyType], [Status], 
    [EffectiveDate], [ExpirationDate], [PremiumAmount], [DeductibleAmount], [CoverageLimit],
    [Description], [PaymentFrequency], [CreatedBy], [ModifiedBy]
)
VALUES 
(NEWID(), 'LIF2024000001', @Client3, 2, 1, 
 '2024-01-15', '2044-01-15', 500.00, 0.00, 1000000.00,
 '20-year term life insurance policy', 'Monthly', 'System', 'System');

GO

PRINT 'Seed data inserted successfully.';
GO
