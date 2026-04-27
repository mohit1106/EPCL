-- =====================================================
-- EPCL Inventory Database Seeder
-- 45 tanks (3 per station) + stock loadings + dip readings
-- =====================================================
USE EPCL_Inventory;
GO

DECLARE @Now DATETIMEOFFSET = SYSDATETIMEOFFSET();
DECLARE @PetrolId UNIQUEIDENTIFIER = 'a1b2c3d4-e5f6-7890-abcd-000000000001';
DECLARE @DieselId UNIQUEIDENTIFIER = 'a1b2c3d4-e5f6-7890-abcd-000000000002';
DECLARE @CNGId    UNIQUEIDENTIFIER = 'a1b2c3d4-e5f6-7890-abcd-000000000003';

-- Get all station IDs from EPCL_Stations
DECLARE @Stations TABLE (StationId UNIQUEIDENTIFIER, StationCode VARCHAR(15), DealerUserId UNIQUEIDENTIFIER);
INSERT INTO @Stations SELECT Id, StationCode, DealerUserId FROM EPCL_Stations.dbo.Stations WHERE StationCode LIKE 'EPCL-%';

IF NOT EXISTS (SELECT 1 FROM Tanks)
BEGIN
    -- Create 3 tanks per station (Petrol 20kL, Diesel 15kL, CNG 10kL)
    INSERT INTO Tanks (Id, StationId, FuelTypeId, TankSerialNumber, CapacityLitres, CurrentStockLitres, ReservedLitres, MinThresholdLitres, Status, LastReplenishedAt, LastDipReadingAt, CreatedAt)
    SELECT NEWID(), s.StationId, @PetrolId,
           'TNK-P-' + RIGHT(s.StationCode,3) + '-01', 20000,
           CAST(12000 + (CHECKSUM(NEWID()) % 6000) AS DECIMAL(10,2)), 0, 3000, 0,
           DATEADD(day, -(ABS(CHECKSUM(NEWID())) % 7), @Now),
           DATEADD(hour, -(ABS(CHECKSUM(NEWID())) % 12), @Now),
           DATEADD(day,-90,@Now)
    FROM @Stations s;

    INSERT INTO Tanks (Id, StationId, FuelTypeId, TankSerialNumber, CapacityLitres, CurrentStockLitres, ReservedLitres, MinThresholdLitres, Status, LastReplenishedAt, LastDipReadingAt, CreatedAt)
    SELECT NEWID(), s.StationId, @DieselId,
           'TNK-D-' + RIGHT(s.StationCode,3) + '-01', 15000,
           CAST(8000 + (CHECKSUM(NEWID()) % 5000) AS DECIMAL(10,2)), 0, 2500, 0,
           DATEADD(day, -(ABS(CHECKSUM(NEWID())) % 7), @Now),
           DATEADD(hour, -(ABS(CHECKSUM(NEWID())) % 12), @Now),
           DATEADD(day,-90,@Now)
    FROM @Stations s;

    INSERT INTO Tanks (Id, StationId, FuelTypeId, TankSerialNumber, CapacityLitres, CurrentStockLitres, ReservedLitres, MinThresholdLitres, Status, LastReplenishedAt, LastDipReadingAt, CreatedAt)
    SELECT NEWID(), s.StationId, @CNGId,
           'TNK-C-' + RIGHT(s.StationCode,3) + '-01', 10000,
           CAST(4000 + (CHECKSUM(NEWID()) % 4000) AS DECIMAL(10,2)), 0, 1500, 0,
           DATEADD(day, -(ABS(CHECKSUM(NEWID())) % 7), @Now),
           DATEADD(hour, -(ABS(CHECKSUM(NEWID())) % 12), @Now),
           DATEADD(day,-90,@Now)
    FROM @Stations s;

    -- Set 3 tanks to Low status, 2 to Critical
    UPDATE TOP(3) Tanks SET Status = 1, CurrentStockLitres = MinThresholdLitres * 0.8 WHERE Status = 0;
    UPDATE TOP(2) Tanks SET Status = 2, CurrentStockLitres = MinThresholdLitres * 0.3 WHERE Status = 0 AND FuelTypeId = @DieselId;

    PRINT 'Tanks created.';
END;

-- Stock Loadings (100 deliveries over 90 days)
IF NOT EXISTS (SELECT 1 FROM StockLoadings)
BEGIN
    ;WITH Nums AS (SELECT TOP 100 ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n FROM sys.objects a CROSS JOIN sys.objects b)
    INSERT INTO StockLoadings (Id, TankId, QuantityLoadedLitres, LoadedByUserId, TankerNumber, InvoiceNumber, SupplierName, StockBefore, StockAfter, Timestamp)
    SELECT NEWID(), t.Id,
           CASE WHEN t.FuelTypeId = @PetrolId THEN 8000 + (ABS(CHECKSUM(NEWID())) % 4000)
                WHEN t.FuelTypeId = @DieselId THEN 6000 + (ABS(CHECKSUM(NEWID())) % 3000)
                ELSE 3000 + (ABS(CHECKSUM(NEWID())) % 2000) END,
           s.DealerUserId,
           'TKR-' + CAST(n.n AS VARCHAR(4)),
           'INV-2024-' + RIGHT('000' + CAST(n.n AS VARCHAR(4)),4),
           CASE (ABS(CHECKSUM(NEWID())) % 3) WHEN 0 THEN 'Indian Oil Corp' WHEN 1 THEN 'BPCL Logistics' ELSE 'HPCL Supply Chain' END,
           t.CurrentStockLitres * 0.4, t.CurrentStockLitres * 0.9,
           DATEADD(day, -(ABS(CHECKSUM(NEWID())) % 90), @Now)
    FROM Nums n
    CROSS APPLY (SELECT TOP 1 Id, FuelTypeId, CurrentStockLitres, StationId FROM Tanks ORDER BY NEWID()) t
    CROSS APPLY (SELECT TOP 1 DealerUserId FROM @Stations WHERE StationId = t.StationId) s;

    PRINT 'StockLoadings created.';
END;

-- Dip Readings (150 readings)
IF NOT EXISTS (SELECT 1 FROM DipReadings)
BEGIN
    ;WITH Nums AS (SELECT TOP 150 ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n FROM sys.objects a CROSS JOIN sys.objects b)
    INSERT INTO DipReadings (Id, TankId, DipValueLitres, SystemStockLitres, VarianceLitres, VariancePercent, IsFraudFlagged, RecordedByUserId, Timestamp)
    SELECT NEWID(), t.Id,
           t.CurrentStockLitres + (CAST(CHECKSUM(NEWID()) AS FLOAT) / 2147483647.0 * 200 - 100),
           t.CurrentStockLitres,
           CAST(CHECKSUM(NEWID()) AS FLOAT) / 2147483647.0 * 200 - 100,
           CAST(CHECKSUM(NEWID()) AS FLOAT) / 2147483647.0 * 2 - 1,
           CASE WHEN ABS(CHECKSUM(NEWID())) % 20 = 0 THEN 1 ELSE 0 END,
           s.DealerUserId,
           DATEADD(hour, -(n.n * 12), @Now)
    FROM Nums n
    CROSS APPLY (SELECT TOP 1 Id, CurrentStockLitres, StationId FROM Tanks ORDER BY NEWID()) t
    CROSS APPLY (SELECT TOP 1 DealerUserId FROM @Stations WHERE StationId = t.StationId) s;

    PRINT 'DipReadings created.';
END;

SELECT 'Tanks' AS Entity, COUNT(*) AS Cnt FROM Tanks
UNION ALL SELECT 'StockLoadings', COUNT(*) FROM StockLoadings
UNION ALL SELECT 'DipReadings', COUNT(*) FROM DipReadings;
GO
