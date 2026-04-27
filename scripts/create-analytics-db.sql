-- EPCL Analytics Database Setup Script
-- Creates the EPCL_Analytics database with read-only views across all service databases.
-- Run against localhost\SQLEXPRESS using Windows Auth.
-- 
-- Prerequisites: All 9 EPCL databases must already exist (Steps 1-12 complete).

-- Step 1: Create the database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'EPCL_Analytics')
    CREATE DATABASE EPCL_Analytics;
GO

USE EPCL_Analytics;
GO

-- Step 2: Create read-only cross-database views
-- These views reference operational databases on the same SQL Server instance.
-- In production, these would be replaced by read replicas or materialized views.

-- View 1: All non-voided transactions with station and pump context
IF OBJECT_ID('vw_Transactions', 'V') IS NOT NULL DROP VIEW vw_Transactions;
GO
CREATE VIEW vw_Transactions AS
SELECT 
    t.Id, t.ReceiptNumber, t.StationId, t.PumpId, t.FuelTypeId,
    t.DealerUserId, t.CustomerUserId, t.VehicleNumber,
    t.QuantityLitres, t.PricePerLitre, t.TotalAmount,
    t.PaymentMethod, t.Status, t.Timestamp,
    t.LoyaltyPointsEarned, t.IsVoided
FROM EPCL_Sales.dbo.Transactions t
WHERE t.IsVoided = 0;
GO

-- View 2: Tank stock levels with percentage calculation
IF OBJECT_ID('vw_TankStockLevels', 'V') IS NOT NULL DROP VIEW vw_TankStockLevels;
GO
CREATE VIEW vw_TankStockLevels AS
SELECT 
    tk.Id AS TankId, tk.StationId, tk.FuelTypeId,
    tk.CurrentStockLitres, tk.CapacityLitres, tk.MinThresholdLitres,
    tk.Status, tk.LastReplenishedAt, tk.UpdatedAt,
    CAST(tk.CurrentStockLitres / NULLIF(tk.CapacityLitres, 0) * 100 AS DECIMAL(5,2)) AS StockPercentage
FROM EPCL_Inventory.dbo.Tanks tk;
GO

-- View 3: Daily sales aggregates for trend analysis
IF OBJECT_ID('vw_DailySales', 'V') IS NOT NULL DROP VIEW vw_DailySales;
GO
CREATE VIEW vw_DailySales AS
SELECT 
    StationId, FuelTypeId, Date,
    TotalTransactions, TotalLitresSold, TotalRevenue,
    LastUpdatedAt
FROM EPCL_Reports.dbo.DailySalesSummaries;
GO

-- View 4: Fraud alerts summary
IF OBJECT_ID('vw_FraudAlerts', 'V') IS NOT NULL DROP VIEW vw_FraudAlerts;
GO
CREATE VIEW vw_FraudAlerts AS
SELECT 
    fa.Id, fa.TransactionId, fa.StationId, fa.RuleTriggered,
    fa.Severity, fa.Status, fa.CreatedAt, fa.ReviewedAt
FROM EPCL_Fraud.dbo.FraudAlerts fa;
GO

-- View 5: Pump activity
IF OBJECT_ID('vw_Pumps', 'V') IS NOT NULL DROP VIEW vw_Pumps;
GO
CREATE VIEW vw_Pumps AS
SELECT 
    p.Id, p.StationId, p.FuelTypeId, p.PumpName,
    p.Status, p.LastServiced, p.NozzleCount
FROM EPCL_Sales.dbo.Pumps p;
GO

-- View 6: Loyalty account summaries
IF OBJECT_ID('vw_LoyaltyAccounts', 'V') IS NOT NULL DROP VIEW vw_LoyaltyAccounts;
GO
CREATE VIEW vw_LoyaltyAccounts AS
SELECT 
    la.CustomerId, la.PointsBalance, la.LifetimePoints,
    la.Tier, la.LastActivityAt
FROM EPCL_Loyalty.dbo.LoyaltyAccounts la;
GO

-- Step 3: Create tables for AI service internal use

-- ConversationHistory — stores chat messages per user session
IF OBJECT_ID('ConversationHistory', 'U') IS NULL
BEGIN
    CREATE TABLE ConversationHistory (
        Id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        UserId          UNIQUEIDENTIFIER NOT NULL,
        SessionId       NVARCHAR(50) NOT NULL,
        Role            VARCHAR(10) NOT NULL,
        Content         NVARCHAR(MAX) NOT NULL,
        GeneratedSql    NVARCHAR(MAX) NULL,
        RowsReturned    INT NULL,
        ExecutionMs     INT NULL,
        CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        INDEX IX_ConvHistory_UserId (UserId),
        INDEX IX_ConvHistory_SessionId (SessionId)
    );
END
GO

-- QueryLog — tracks queries for monitoring and cost analysis
IF OBJECT_ID('QueryLog', 'U') IS NULL
BEGIN
    CREATE TABLE QueryLog (
        Id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        UserId          UNIQUEIDENTIFIER NOT NULL,
        UserRole        NVARCHAR(20) NOT NULL,
        Question        NVARCHAR(2000) NOT NULL,
        GeneratedSql    NVARCHAR(MAX) NULL,
        WasSqlValid     BIT NOT NULL DEFAULT 0,
        RowsReturned    INT NULL,
        GeminiTokensUsed INT NULL,
        TotalMs         INT NOT NULL,
        WasSuccessful   BIT NOT NULL DEFAULT 1,
        ErrorMessage    NVARCHAR(500) NULL,
        CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
END
GO

PRINT 'EPCL_Analytics database setup complete.';
PRINT 'Views created: vw_Transactions, vw_TankStockLevels, vw_DailySales, vw_FraudAlerts, vw_Pumps, vw_LoyaltyAccounts';
PRINT 'Tables created: ConversationHistory, QueryLog';
GO
