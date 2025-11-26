-- Stored Procedures for Expense Management System
-- All database operations should go through these procedures

-- ================================================================
-- EXPENSE PROCEDURES
-- ================================================================

-- Get all expenses (with optional filtering)
CREATE OR ALTER PROCEDURE [dbo].[sp_GetExpenses]
    @UserId INT = NULL,
    @StatusId INT = NULL,
    @FromDate DATE = NULL,
    @ToDate DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        e.ExpenseId,
        e.UserId,
        u.UserName,
        u.Email,
        e.CategoryId,
        ec.CategoryName,
        e.StatusId,
        es.StatusName,
        e.AmountMinor,
        e.Currency,
        e.ExpenseDate,
        e.Description,
        e.ReceiptFile,
        e.SubmittedAt,
        e.ReviewedBy,
        r.UserName AS ReviewerName,
        e.ReviewedAt,
        e.CreatedAt
    FROM dbo.Expenses e
    INNER JOIN dbo.Users u ON e.UserId = u.UserId
    INNER JOIN dbo.ExpenseCategories ec ON e.CategoryId = ec.CategoryId
    INNER JOIN dbo.ExpenseStatus es ON e.StatusId = es.StatusId
    LEFT JOIN dbo.Users r ON e.ReviewedBy = r.UserId
    WHERE (@UserId IS NULL OR e.UserId = @UserId)
      AND (@StatusId IS NULL OR e.StatusId = @StatusId)
      AND (@FromDate IS NULL OR e.ExpenseDate >= @FromDate)
      AND (@ToDate IS NULL OR e.ExpenseDate <= @ToDate)
    ORDER BY e.CreatedAt DESC;
END
GO

-- Get a single expense by ID
CREATE OR ALTER PROCEDURE [dbo].[sp_GetExpenseById]
    @ExpenseId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        e.ExpenseId,
        e.UserId,
        u.UserName,
        u.Email,
        e.CategoryId,
        ec.CategoryName,
        e.StatusId,
        es.StatusName,
        e.AmountMinor,
        e.Currency,
        e.ExpenseDate,
        e.Description,
        e.ReceiptFile,
        e.SubmittedAt,
        e.ReviewedBy,
        r.UserName AS ReviewerName,
        e.ReviewedAt,
        e.CreatedAt
    FROM dbo.Expenses e
    INNER JOIN dbo.Users u ON e.UserId = u.UserId
    INNER JOIN dbo.ExpenseCategories ec ON e.CategoryId = ec.CategoryId
    INNER JOIN dbo.ExpenseStatus es ON e.StatusId = es.StatusId
    LEFT JOIN dbo.Users r ON e.ReviewedBy = r.UserId
    WHERE e.ExpenseId = @ExpenseId;
END
GO

-- Create a new expense
CREATE OR ALTER PROCEDURE [dbo].[sp_CreateExpense]
    @UserId INT,
    @CategoryId INT,
    @AmountMinor INT,
    @Currency NVARCHAR(3) = 'GBP',
    @ExpenseDate DATE,
    @Description NVARCHAR(1000) = NULL,
    @ReceiptFile NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @StatusId INT;
    SELECT @StatusId = StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Draft';
    
    INSERT INTO dbo.Expenses (UserId, CategoryId, StatusId, AmountMinor, Currency, ExpenseDate, Description, ReceiptFile)
    VALUES (@UserId, @CategoryId, @StatusId, @AmountMinor, @Currency, @ExpenseDate, @Description, @ReceiptFile);
    
    SELECT SCOPE_IDENTITY() AS ExpenseId;
END
GO

-- Update an expense
CREATE OR ALTER PROCEDURE [dbo].[sp_UpdateExpense]
    @ExpenseId INT,
    @CategoryId INT,
    @AmountMinor INT,
    @ExpenseDate DATE,
    @Description NVARCHAR(1000) = NULL,
    @ReceiptFile NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE dbo.Expenses
    SET CategoryId = @CategoryId,
        AmountMinor = @AmountMinor,
        ExpenseDate = @ExpenseDate,
        Description = @Description,
        ReceiptFile = @ReceiptFile
    WHERE ExpenseId = @ExpenseId;
    
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- Submit an expense
CREATE OR ALTER PROCEDURE [dbo].[sp_SubmitExpense]
    @ExpenseId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @StatusId INT;
    SELECT @StatusId = StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Submitted';
    
    UPDATE dbo.Expenses
    SET StatusId = @StatusId,
        SubmittedAt = SYSUTCDATETIME()
    WHERE ExpenseId = @ExpenseId;
    
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- Approve an expense
CREATE OR ALTER PROCEDURE [dbo].[sp_ApproveExpense]
    @ExpenseId INT,
    @ReviewerId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @StatusId INT;
    SELECT @StatusId = StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Approved';
    
    UPDATE dbo.Expenses
    SET StatusId = @StatusId,
        ReviewedBy = @ReviewerId,
        ReviewedAt = SYSUTCDATETIME()
    WHERE ExpenseId = @ExpenseId;
    
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- Reject an expense
CREATE OR ALTER PROCEDURE [dbo].[sp_RejectExpense]
    @ExpenseId INT,
    @ReviewerId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @StatusId INT;
    SELECT @StatusId = StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Rejected';
    
    UPDATE dbo.Expenses
    SET StatusId = @StatusId,
        ReviewedBy = @ReviewerId,
        ReviewedAt = SYSUTCDATETIME()
    WHERE ExpenseId = @ExpenseId;
    
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- Delete an expense
CREATE OR ALTER PROCEDURE [dbo].[sp_DeleteExpense]
    @ExpenseId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DELETE FROM dbo.Expenses
    WHERE ExpenseId = @ExpenseId;
    
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- ================================================================
-- USER PROCEDURES
-- ================================================================

-- Get all users
CREATE OR ALTER PROCEDURE [dbo].[sp_GetUsers]
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        u.UserId,
        u.UserName,
        u.Email,
        u.RoleId,
        r.RoleName,
        u.ManagerId,
        m.UserName AS ManagerName,
        u.IsActive,
        u.CreatedAt
    FROM dbo.Users u
    INNER JOIN dbo.Roles r ON u.RoleId = r.RoleId
    LEFT JOIN dbo.Users m ON u.ManagerId = m.UserId
    WHERE u.IsActive = 1
    ORDER BY u.UserName;
END
GO

-- Get a single user by ID
CREATE OR ALTER PROCEDURE [dbo].[sp_GetUserById]
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        u.UserId,
        u.UserName,
        u.Email,
        u.RoleId,
        r.RoleName,
        u.ManagerId,
        m.UserName AS ManagerName,
        u.IsActive,
        u.CreatedAt
    FROM dbo.Users u
    INNER JOIN dbo.Roles r ON u.RoleId = r.RoleId
    LEFT JOIN dbo.Users m ON u.ManagerId = m.UserId
    WHERE u.UserId = @UserId;
END
GO

-- Create a new user
CREATE OR ALTER PROCEDURE [dbo].[sp_CreateUser]
    @UserName NVARCHAR(100),
    @Email NVARCHAR(255),
    @RoleId INT,
    @ManagerId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO dbo.Users (UserName, Email, RoleId, ManagerId)
    VALUES (@UserName, @Email, @RoleId, @ManagerId);
    
    SELECT SCOPE_IDENTITY() AS UserId;
END
GO

-- Update a user
CREATE OR ALTER PROCEDURE [dbo].[sp_UpdateUser]
    @UserId INT,
    @UserName NVARCHAR(100),
    @Email NVARCHAR(255),
    @RoleId INT,
    @ManagerId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE dbo.Users
    SET UserName = @UserName,
        Email = @Email,
        RoleId = @RoleId,
        ManagerId = @ManagerId
    WHERE UserId = @UserId;
    
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- ================================================================
-- CATEGORY PROCEDURES
-- ================================================================

-- Get all expense categories
CREATE OR ALTER PROCEDURE [dbo].[sp_GetExpenseCategories]
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT CategoryId, CategoryName, IsActive
    FROM dbo.ExpenseCategories
    WHERE IsActive = 1
    ORDER BY CategoryName;
END
GO

-- ================================================================
-- STATUS PROCEDURES
-- ================================================================

-- Get all expense statuses
CREATE OR ALTER PROCEDURE [dbo].[sp_GetExpenseStatuses]
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT StatusId, StatusName
    FROM dbo.ExpenseStatus
    ORDER BY StatusId;
END
GO

-- ================================================================
-- ROLE PROCEDURES
-- ================================================================

-- Get all roles
CREATE OR ALTER PROCEDURE [dbo].[sp_GetRoles]
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT RoleId, RoleName, Description
    FROM dbo.Roles
    ORDER BY RoleName;
END
GO

-- ================================================================
-- REPORTING PROCEDURES
-- ================================================================

-- Get expense summary by user
CREATE OR ALTER PROCEDURE [dbo].[sp_GetExpenseSummaryByUser]
    @UserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        u.UserId,
        u.UserName,
        es.StatusName,
        COUNT(*) AS ExpenseCount,
        SUM(e.AmountMinor) AS TotalAmountMinor,
        e.Currency
    FROM dbo.Expenses e
    INNER JOIN dbo.Users u ON e.UserId = u.UserId
    INNER JOIN dbo.ExpenseStatus es ON e.StatusId = es.StatusId
    WHERE (@UserId IS NULL OR e.UserId = @UserId)
    GROUP BY u.UserId, u.UserName, es.StatusName, e.Currency
    ORDER BY u.UserName, es.StatusName;
END
GO

-- Get expense summary by category
CREATE OR ALTER PROCEDURE [dbo].[sp_GetExpenseSummaryByCategory]
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        ec.CategoryName,
        COUNT(*) AS ExpenseCount,
        SUM(e.AmountMinor) AS TotalAmountMinor,
        e.Currency
    FROM dbo.Expenses e
    INNER JOIN dbo.ExpenseCategories ec ON e.CategoryId = ec.CategoryId
    GROUP BY ec.CategoryName, e.Currency
    ORDER BY ec.CategoryName;
END
GO
