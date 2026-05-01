-- ============================================
-- Stored Procedures for Agency Management System
-- ============================================

-- ============================================
-- Procedure: Generate Policy Number
-- ============================================
CREATE OR ALTER PROCEDURE [dbo].[usp_GeneratePolicyNumber]
    @PolicyType INT,
    @PolicyNumber NVARCHAR(50) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Prefix NVARCHAR(10);
    DECLARE @Sequence INT;
    DECLARE @Year NVARCHAR(4) = CAST(YEAR(GETUTCDATE()) AS NVARCHAR(4));
    
    -- Set prefix based on policy type
    SET @Prefix = CASE @PolicyType
        WHEN 0 THEN 'AUT'  -- Auto
        WHEN 1 THEN 'HOM'  -- Home
        WHEN 2 THEN 'LIF'  -- Life
        WHEN 3 THEN 'HLT'  -- Health
        WHEN 4 THEN 'BUS'  -- Business
        WHEN 5 THEN 'TRV'  -- Travel
        WHEN 6 THEN 'PET'  -- Pet
        WHEN 7 THEN 'UMB'  -- Umbrella
        WHEN 8 THEN 'RNT'  -- Renters
        WHEN 9 THEN 'OTH'  -- Other
        ELSE 'GEN'
    END;
    
    -- Get next sequence number
    SELECT @Sequence = ISNULL(MAX(CAST(SUBSTRING(PolicyNumber, 8, 6) AS INT)), 0) + 1
    FROM [dbo].[Policy]
    WHERE PolicyNumber LIKE @Prefix + @Year + '%';
    
    -- Generate policy number: PREFIX + YEAR + 6-digit sequence
    SET @PolicyNumber = @Prefix + @Year + RIGHT('000000' + CAST(@Sequence AS NVARCHAR(6)), 6);
END
GO

-- ============================================
-- Procedure: Generate Claim Number
-- ============================================
CREATE OR ALTER PROCEDURE [dbo].[usp_GenerateClaimNumber]
    @ClaimNumber NVARCHAR(50) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Year NVARCHAR(4) = CAST(YEAR(GETUTCDATE()) AS NVARCHAR(4));
    DECLARE @Month NVARCHAR(2) = RIGHT('00' + CAST(MONTH(GETUTCDATE()) AS NVARCHAR(2)), 2);
    DECLARE @Sequence INT;
    
    -- Get next sequence number for this month
    SELECT @Sequence = ISNULL(MAX(CAST(RIGHT(ClaimNumber, 5) AS INT)), 0) + 1
    FROM [dbo].[Claim]
    WHERE ClaimNumber LIKE 'CLM' + @Year + @Month + '%';
    
    -- Generate claim number: CLM + YYYYMM + 5-digit sequence
    SET @ClaimNumber = 'CLM' + @Year + @Month + RIGHT('00000' + CAST(@Sequence AS NVARCHAR(5)), 5);
END
GO

-- ============================================
-- Procedure: Get Client Summary
-- ============================================
CREATE OR ALTER PROCEDURE [dbo].[usp_GetClientSummary]
    @ClientId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        c.Id,
        c.FirstName,
        c.LastName,
        c.Email,
        c.PhoneNumber,
        TotalPolicies = (SELECT COUNT(*) FROM [dbo].[Policy] WHERE ClientId = @ClientId),
        ActivePolicies = (SELECT COUNT(*) FROM [dbo].[Policy] WHERE ClientId = @ClientId AND Status = 1),
        TotalClaims = (SELECT COUNT(*) FROM [dbo].[Claim] WHERE ClientId = @ClientId),
        OpenClaims = (SELECT COUNT(*) FROM [dbo].[Claim] WHERE ClientId = @ClientId AND Status NOT IN (8, 9, 11)), -- Not Closed, Settled, or Withdrawn
        TotalPremium = (SELECT ISNULL(SUM(PremiumAmount), 0) FROM [dbo].[Policy] WHERE ClientId = @ClientId AND Status = 1),
        TotalClaimAmount = (SELECT ISNULL(SUM(ClaimAmount), 0) FROM [dbo].[Claim] WHERE ClientId = @ClientId),
        TotalPaidAmount = (SELECT ISNULL(SUM(PaidAmount), 0) FROM [dbo].[Claim] WHERE ClientId = @ClientId),
        ClientSince = c.CreatedAt
    FROM [dbo].[Client] c
    WHERE c.Id = @ClientId;
END
GO

-- ============================================
-- Procedure: Get Expiring Policies
-- ============================================
CREATE OR ALTER PROCEDURE [dbo].[usp_GetExpiringPolicies]
    @DaysUntilExpiration INT = 30
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        p.Id,
        p.PolicyNumber,
        p.PolicyType,
        p.Status,
        p.EffectiveDate,
        p.ExpirationDate,
        p.PremiumAmount,
        p.CoverageLimit,
        DaysUntilExpiration = DATEDIFF(DAY, GETUTCDATE(), p.ExpirationDate),
        c.Id AS ClientId,
        c.FirstName,
        c.LastName,
        c.Email,
        c.PhoneNumber
    FROM [dbo].[Policy] p
    INNER JOIN [dbo].[Client] c ON p.ClientId = c.Id
    WHERE p.Status = 1  -- Active
      AND p.ExpirationDate BETWEEN GETUTCDATE() AND DATEADD(DAY, @DaysUntilExpiration, GETUTCDATE())
    ORDER BY p.ExpirationDate ASC;
END
GO

-- ============================================
-- Procedure: Get Claims Dashboard
-- ============================================
CREATE OR ALTER PROCEDURE [dbo].[usp_GetClaimsDashboard]
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Claims summary by status
    SELECT 
        Status,
        TotalCount = COUNT(*),
        TotalAmount = SUM(ClaimAmount),
        AvgAmount = AVG(ClaimAmount)
    FROM [dbo].[Claim]
    GROUP BY Status;
    
    -- Claims by month (last 12 months)
    SELECT 
        YearMonth = FORMAT(DateReported, 'yyyy-MM'),
        TotalClaims = COUNT(*),
        TotalAmount = SUM(ClaimAmount),
        ApprovedAmount = SUM(ApprovedAmount),
        PaidAmount = SUM(PaidAmount)
    FROM [dbo].[Claim]
    WHERE DateReported >= DATEADD(MONTH, -12, GETUTCDATE())
    GROUP BY FORMAT(DateReported, 'yyyy-MM')
    ORDER BY YearMonth;
    
    -- Claims by policy type
    SELECT 
        p.PolicyType,
        TotalClaims = COUNT(cl.Id),
        TotalAmount = SUM(cl.ClaimAmount),
        AvgProcessingDays = AVG(DATEDIFF(DAY, cl.DateReported, ISNULL(cl.SettlementDate, GETUTCDATE())))
    FROM [dbo].[Claim] cl
    INNER JOIN [dbo].[Policy] p ON cl.PolicyId = p.Id
    GROUP BY p.PolicyType;
    
    -- Top adjusters by claim count
    SELECT TOP 10
        AdjusterId,
        AdjusterName,
        TotalClaims = COUNT(*),
        ClosedClaims = SUM(CASE WHEN Status IN (8, 9) THEN 1 ELSE 0 END),
        AvgProcessingDays = AVG(DATEDIFF(DAY, AssignedDate, ISNULL(ClosedDate, GETUTCDATE())))
    FROM [dbo].[Claim]
    WHERE AdjusterId IS NOT NULL
    GROUP BY AdjusterId, AdjusterName
    ORDER BY TotalClaims DESC;
END
GO

-- ============================================
-- Procedure: Search Clients
-- ============================================
CREATE OR ALTER PROCEDURE [dbo].[usp_SearchClients]
    @SearchTerm NVARCHAR(200) = NULL,
    @IsActive BIT = NULL,
    @PageNumber INT = 1,
    @PageSize INT = 20,
    @SortBy NVARCHAR(50) = 'LastName',
    @Ascending BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;
    
    -- Get total count
    SELECT COUNT(*) AS TotalCount
    FROM [dbo].[Client] c
    WHERE (@SearchTerm IS NULL OR 
           c.FirstName LIKE '%' + @SearchTerm + '%' OR
           c.LastName LIKE '%' + @SearchTerm + '%' OR
           c.Email LIKE '%' + @SearchTerm + '%' OR
           c.PhoneNumber LIKE '%' + @SearchTerm + '%')
      AND (@IsActive IS NULL OR c.IsActive = @IsActive);
    
    -- Get paginated results
    SELECT 
        c.*,
        ActivePolicies = (SELECT COUNT(*) FROM [dbo].[Policy] WHERE ClientId = c.Id AND Status = 1),
        OpenClaims = (SELECT COUNT(*) FROM [dbo].[Claim] WHERE ClientId = c.Id AND Status NOT IN (8, 9, 11))
    FROM [dbo].[Client] c
    WHERE (@SearchTerm IS NULL OR 
           c.FirstName LIKE '%' + @SearchTerm + '%' OR
           c.LastName LIKE '%' + @SearchTerm + '%' OR
           c.Email LIKE '%' + @SearchTerm + '%' OR
           c.PhoneNumber LIKE '%' + @SearchTerm + '%')
      AND (@IsActive IS NULL OR c.IsActive = @IsActive)
    ORDER BY 
        CASE WHEN @SortBy = 'FirstName' AND @Ascending = 1 THEN c.FirstName END ASC,
        CASE WHEN @SortBy = 'FirstName' AND @Ascending = 0 THEN c.FirstName END DESC,
        CASE WHEN @SortBy = 'LastName' AND @Ascending = 1 THEN c.LastName END ASC,
        CASE WHEN @SortBy = 'LastName' AND @Ascending = 0 THEN c.LastName END DESC,
        CASE WHEN @SortBy = 'Email' AND @Ascending = 1 THEN c.Email END ASC,
        CASE WHEN @SortBy = 'Email' AND @Ascending = 0 THEN c.Email END DESC,
        CASE WHEN @SortBy = 'CreatedAt' AND @Ascending = 1 THEN c.CreatedAt END ASC,
        CASE WHEN @SortBy = 'CreatedAt' AND @Ascending = 0 THEN c.CreatedAt END DESC,
        c.LastName ASC
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO

-- ============================================
-- Procedure: Process Claim Payment
-- ============================================
CREATE OR ALTER PROCEDURE [dbo].[usp_ProcessClaimPayment]
    @ClaimId UNIQUEIDENTIFIER,
    @PaymentAmount DECIMAL(18,2),
    @UserId NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    
    BEGIN TRY
        DECLARE @CurrentPaidAmount DECIMAL(18,2);
        DECLARE @ApprovedAmount DECIMAL(18,2);
        DECLARE @NewPaidAmount DECIMAL(18,2);
        
        SELECT @CurrentPaidAmount = ISNULL(PaidAmount, 0), @ApprovedAmount = ApprovedAmount
        FROM [dbo].[Claim] WITH (UPDLOCK)
        WHERE Id = @ClaimId;
        
        SET @NewPaidAmount = @CurrentPaidAmount + @PaymentAmount;
        
        -- Validate payment doesn't exceed approved amount
        IF @ApprovedAmount IS NOT NULL AND @NewPaidAmount > @ApprovedAmount
        BEGIN
            RAISERROR('Payment would exceed approved amount', 16, 1);
            RETURN;
        END
        
        -- Update claim
        UPDATE [dbo].[Claim]
        SET PaidAmount = @NewPaidAmount,
            Status = CASE 
                WHEN @NewPaidAmount >= @ApprovedAmount THEN 9  -- Settled
                ELSE Status
            END,
            SettlementDate = CASE 
                WHEN @NewPaidAmount >= @ApprovedAmount THEN GETUTCDATE()
                ELSE SettlementDate
            END,
            ModifiedAt = GETUTCDATE(),
            ModifiedBy = @UserId
        WHERE Id = @ClaimId;
        
        COMMIT TRANSACTION;
        
        SELECT 'SUCCESS' AS Result, @NewPaidAmount AS NewPaidAmount;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

PRINT 'Stored procedures created successfully.';
GO
