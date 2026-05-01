-- ============================================
-- Agency Management System Database Schema
-- SQL Server / Azure SQL Database
-- ============================================

-- Create database (if running locally)
-- CREATE DATABASE AgencyManagementSystem;
-- GO
-- USE AgencyManagementSystem;
-- GO

-- ============================================
-- Schema: dbo (default)
-- ============================================

-- ============================================
-- Table: Role
-- ============================================
CREATE TABLE [dbo].[Role] (
    [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    [Name] NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    [Permissions] NVARCHAR(MAX) NULL,  -- JSON array of permissions
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    [CreatedBy] NVARCHAR(100) NOT NULL,
    [ModifiedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    [ModifiedBy] NVARCHAR(100) NOT NULL,
    [RowVersion] ROWVERSION NOT NULL,
    
    CONSTRAINT [PK_Role] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [UQ_Role_Name] UNIQUE ([Name])
);

CREATE INDEX [IX_Role_IsActive] ON [dbo].[Role] ([IsActive]);
GO

-- ============================================
-- Table: User
-- ============================================
CREATE TABLE [dbo].[User] (
    [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    [Username] NVARCHAR(100) NOT NULL,
    [Email] NVARCHAR(255) NOT NULL,
    [PasswordHash] NVARCHAR(500) NOT NULL,
    [PasswordSalt] NVARCHAR(500) NOT NULL,
    [FirstName] NVARCHAR(100) NOT NULL,
    [LastName] NVARCHAR(100) NOT NULL,
    [PhoneNumber] NVARCHAR(20) NULL,
    [RoleId] UNIQUEIDENTIFIER NOT NULL,
    [Department] NVARCHAR(100) NULL,
    [JobTitle] NVARCHAR(100) NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [IsEmailVerified] BIT NOT NULL DEFAULT 0,
    [IsTwoFactorEnabled] BIT NOT NULL DEFAULT 0,
    [TwoFactorSecret] NVARCHAR(200) NULL,
    [LastLoginAt] DATETIME2(7) NULL,
    [FailedLoginAttempts] INT NOT NULL DEFAULT 0,
    [LockoutEnd] DATETIME2(7) NULL,
    [RefreshToken] NVARCHAR(500) NULL,
    [RefreshTokenExpiry] DATETIME2(7) NULL,
    [PasswordResetToken] NVARCHAR(500) NULL,
    [PasswordResetExpiry] DATETIME2(7) NULL,
    [ProfilePictureUrl] NVARCHAR(500) NULL,
    [Preferences] NVARCHAR(MAX) NULL,  -- JSON preferences
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    [CreatedBy] NVARCHAR(100) NOT NULL,
    [ModifiedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    [ModifiedBy] NVARCHAR(100) NOT NULL,
    [RowVersion] ROWVERSION NOT NULL,
    
    CONSTRAINT [PK_User] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [UQ_User_Username] UNIQUE ([Username]),
    CONSTRAINT [UQ_User_Email] UNIQUE ([Email]),
    CONSTRAINT [FK_User_Role] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Role]([Id])
);

CREATE INDEX [IX_User_RoleId] ON [dbo].[User] ([RoleId]);
CREATE INDEX [IX_User_IsActive] ON [dbo].[User] ([IsActive]);
CREATE INDEX [IX_User_Email] ON [dbo].[User] ([Email]);
GO

-- ============================================
-- Table: Client
-- ============================================
CREATE TABLE [dbo].[Client] (
    [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    [FirstName] NVARCHAR(100) NOT NULL,
    [LastName] NVARCHAR(100) NOT NULL,
    [Email] NVARCHAR(255) NOT NULL,
    [PhoneNumber] NVARCHAR(20) NULL,
    [AlternatePhone] NVARCHAR(20) NULL,
    [Address] NVARCHAR(500) NULL,
    [City] NVARCHAR(100) NULL,
    [State] NVARCHAR(50) NULL,
    [ZipCode] NVARCHAR(20) NULL,
    [Country] NVARCHAR(100) NULL DEFAULT 'USA',
    [DateOfBirth] DATE NOT NULL,
    [SocialSecurityNumber] NVARCHAR(50) NULL,  -- Encrypted in production
    [DriversLicense] NVARCHAR(50) NULL,
    [Occupation] NVARCHAR(200) NULL,
    [Employer] NVARCHAR(200) NULL,
    [AnnualIncome] DECIMAL(18,2) NULL,
    [PreferredContactMethod] NVARCHAR(50) NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [Notes] NVARCHAR(MAX) NULL,
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    [CreatedBy] NVARCHAR(100) NOT NULL,
    [ModifiedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    [ModifiedBy] NVARCHAR(100) NOT NULL,
    [RowVersion] ROWVERSION NOT NULL,
    
    CONSTRAINT [PK_Client] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [UQ_Client_Email] UNIQUE ([Email])
);

CREATE INDEX [IX_Client_Name] ON [dbo].[Client] ([LastName], [FirstName]);
CREATE INDEX [IX_Client_Email] ON [dbo].[Client] ([Email]);
CREATE INDEX [IX_Client_IsActive] ON [dbo].[Client] ([IsActive]);
GO

-- ============================================
-- Table: Policy
-- ============================================
CREATE TABLE [dbo].[Policy] (
    [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    [PolicyNumber] NVARCHAR(50) NOT NULL,
    [ClientId] UNIQUEIDENTIFIER NOT NULL,
    [PolicyType] INT NOT NULL,  -- Enum: Auto=0, Home=1, Life=2, etc.
    [Status] INT NOT NULL DEFAULT 0,  -- Enum: Pending=0, Active=1, etc.
    [EffectiveDate] DATE NOT NULL,
    [ExpirationDate] DATE NOT NULL,
    [PremiumAmount] DECIMAL(18,2) NOT NULL,
    [DeductibleAmount] DECIMAL(18,2) NOT NULL,
    [CoverageLimit] DECIMAL(18,2) NOT NULL,
    [Description] NVARCHAR(1000) NULL,
    [UnderwriterId] NVARCHAR(100) NULL,
    [PaymentFrequency] NVARCHAR(50) NOT NULL,  -- Monthly, Quarterly, etc.
    [LastPaymentDate] DATE NULL,
    [NextPaymentDate] DATE NULL,
    [CoverageDetails] NVARCHAR(MAX) NULL,  -- JSON coverage details
    [Exclusions] NVARCHAR(MAX) NULL,  -- JSON exclusions
    [Endorsements] NVARCHAR(MAX) NULL,  -- JSON endorsements
    [RenewalCount] INT NOT NULL DEFAULT 0,
    [Notes] NVARCHAR(MAX) NULL,
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    [CreatedBy] NVARCHAR(100) NOT NULL,
    [ModifiedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    [ModifiedBy] NVARCHAR(100) NOT NULL,
    [RowVersion] ROWVERSION NOT NULL,
    
    CONSTRAINT [PK_Policy] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [UQ_Policy_PolicyNumber] UNIQUE ([PolicyNumber]),
    CONSTRAINT [FK_Policy_Client] FOREIGN KEY ([ClientId]) REFERENCES [dbo].[Client]([Id]),
    CONSTRAINT [CK_Policy_Dates] CHECK ([ExpirationDate] > [EffectiveDate]),
    CONSTRAINT [CK_Policy_Premium] CHECK ([PremiumAmount] >= 0),
    CONSTRAINT [CK_Policy_Deductible] CHECK ([DeductibleAmount] >= 0),
    CONSTRAINT [CK_Policy_Coverage] CHECK ([CoverageLimit] > 0)
);

CREATE INDEX [IX_Policy_ClientId] ON [dbo].[Policy] ([ClientId]);
CREATE INDEX [IX_Policy_PolicyNumber] ON [dbo].[Policy] ([PolicyNumber]);
CREATE INDEX [IX_Policy_Status] ON [dbo].[Policy] ([Status]);
CREATE INDEX [IX_Policy_PolicyType] ON [dbo].[Policy] ([PolicyType]);
CREATE INDEX [IX_Policy_ExpirationDate] ON [dbo].[Policy] ([ExpirationDate]);
CREATE INDEX [IX_Policy_DateRange] ON [dbo].[Policy] ([EffectiveDate], [ExpirationDate]);
GO

-- ============================================
-- Table: Claim
-- ============================================
CREATE TABLE [dbo].[Claim] (
    [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    [ClaimNumber] NVARCHAR(50) NOT NULL,
    [PolicyId] UNIQUEIDENTIFIER NOT NULL,
    [ClientId] UNIQUEIDENTIFIER NOT NULL,
    [Status] INT NOT NULL DEFAULT 0,  -- Enum: Submitted=0, UnderInvestigation=1, etc.
    [DateOfLoss] DATE NOT NULL,
    [DateReported] DATE NOT NULL DEFAULT CAST(GETUTCDATE() AS DATE),
    [Description] NVARCHAR(MAX) NOT NULL,
    [ClaimAmount] DECIMAL(18,2) NOT NULL,
    [ApprovedAmount] DECIMAL(18,2) NULL,
    [PaidAmount] DECIMAL(18,2) NULL DEFAULT 0,
    [DeductibleApplied] DECIMAL(18,2) NULL,
    [LossLocation] NVARCHAR(500) NULL,
    [LossType] NVARCHAR(100) NULL,
    [CauseOfLoss] NVARCHAR(500) NULL,
    [AdjusterId] NVARCHAR(100) NULL,
    [AdjusterName] NVARCHAR(200) NULL,
    [AdjusterNotes] NVARCHAR(MAX) NULL,
    [AssignedDate] DATETIME2(7) NULL,
    [InvestigationStartDate] DATETIME2(7) NULL,
    [InvestigationEndDate] DATETIME2(7) NULL,
    [SettlementDate] DATETIME2(7) NULL,
    [ClosedDate] DATETIME2(7) NULL,
    [Documents] NVARCHAR(MAX) NULL,  -- JSON array of document references
    [WitnessInformation] NVARCHAR(MAX) NULL,
    [PoliceReportNumber] NVARCHAR(100) NULL,
    [PoliceReportDate] DATE NULL,
    [DenialReason] NVARCHAR(1000) NULL,
    [IsFraudulent] BIT NOT NULL DEFAULT 0,
    [Priority] INT NOT NULL DEFAULT 1,  -- 1=Low, 2=Medium, 3=High, 4=Critical
    [Notes] NVARCHAR(MAX) NULL,
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    [CreatedBy] NVARCHAR(100) NOT NULL,
    [ModifiedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    [ModifiedBy] NVARCHAR(100) NOT NULL,
    [RowVersion] ROWVERSION NOT NULL,
    
    CONSTRAINT [PK_Claim] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [UQ_Claim_ClaimNumber] UNIQUE ([ClaimNumber]),
    CONSTRAINT [FK_Claim_Policy] FOREIGN KEY ([PolicyId]) REFERENCES [dbo].[Policy]([Id]),
    CONSTRAINT [FK_Claim_Client] FOREIGN KEY ([ClientId]) REFERENCES [dbo].[Client]([Id]),
    CONSTRAINT [CK_Claim_Amount] CHECK ([ClaimAmount] > 0),
    CONSTRAINT [CK_Claim_ApprovedAmount] CHECK ([ApprovedAmount] IS NULL OR [ApprovedAmount] >= 0)
);

CREATE INDEX [IX_Claim_PolicyId] ON [dbo].[Claim] ([PolicyId]);
CREATE INDEX [IX_Claim_ClientId] ON [dbo].[Claim] ([ClientId]);
CREATE INDEX [IX_Claim_ClaimNumber] ON [dbo].[Claim] ([ClaimNumber]);
CREATE INDEX [IX_Claim_Status] ON [dbo].[Claim] ([Status]);
CREATE INDEX [IX_Claim_DateOfLoss] ON [dbo].[Claim] ([DateOfLoss]);
CREATE INDEX [IX_Claim_DateReported] ON [dbo].[Claim] ([DateReported]);
CREATE INDEX [IX_Claim_AdjusterId] ON [dbo].[Claim] ([AdjusterId]);
GO

-- ============================================
-- Table: AuditLog
-- ============================================
CREATE TABLE [dbo].[AuditLog] (
    [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    [EntityName] NVARCHAR(200) NOT NULL,
    [EntityId] NVARCHAR(100) NOT NULL,
    [Action] NVARCHAR(50) NOT NULL,  -- Created, Updated, Deleted, etc.
    [UserId] NVARCHAR(100) NULL,
    [Timestamp] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    [OldValues] NVARCHAR(MAX) NULL,  -- JSON
    [NewValues] NVARCHAR(MAX) NULL,  -- JSON
    [IpAddress] NVARCHAR(50) NULL,
    [UserAgent] NVARCHAR(500) NULL,
    [AdditionalInfo] NVARCHAR(MAX) NULL,  -- JSON
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    [CreatedBy] NVARCHAR(100) NOT NULL,
    [ModifiedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    [ModifiedBy] NVARCHAR(100) NOT NULL,
    [RowVersion] ROWVERSION NOT NULL,
    
    CONSTRAINT [PK_AuditLog] PRIMARY KEY CLUSTERED ([Id])
);

CREATE INDEX [IX_AuditLog_EntityName] ON [dbo].[AuditLog] ([EntityName]);
CREATE INDEX [IX_AuditLog_EntityId] ON [dbo].[AuditLog] ([EntityId]);
CREATE INDEX [IX_AuditLog_UserId] ON [dbo].[AuditLog] ([UserId]);
CREATE INDEX [IX_AuditLog_Timestamp] ON [dbo].[AuditLog] ([Timestamp]);
CREATE INDEX [IX_AuditLog_Action] ON [dbo].[AuditLog] ([Action]);
CREATE INDEX [IX_AuditLog_Entity] ON [dbo].[AuditLog] ([EntityName], [EntityId]);
CREATE INDEX [IX_AuditLog_UserActivity] ON [dbo].[AuditLog] ([UserId], [Timestamp]);
GO

PRINT 'Schema created successfully.';
GO
