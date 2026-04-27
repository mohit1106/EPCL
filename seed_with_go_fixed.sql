SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- EPCL_Stations: Seed FuelTypes
USE EPCL_Stations;
IF NOT EXISTS (SELECT 1 FROM FuelTypes)
BEGIN
    INSERT INTO FuelTypes (Id, Name, Description, IsActive) VALUES
    (NEWID(), 'Petrol',        'Regular unleaded petrol (87 octane)',     1),
    (NEWID(), 'Diesel',        'High-speed diesel (HSD)',                 1),
    (NEWID(), 'CNG',           'Compressed Natural Gas',                  1),
    (NEWID(), 'PremiumPetrol', 'High-octane premium petrol (97+ octane)', 1),
    (NEWID(), 'PremiumDiesel', 'Premium diesel with performance additives',1);
END
GO

-- EPCL_Identity: Seed SuperAdmin user
USE EPCL_Identity;
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'admin@epcl.in')
BEGIN
    DECLARE @AdminId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Users (Id, FullName, Email, PhoneNumber, PasswordHash, Role, 
                       IsActive, FailedLoginAttempts, AuthProvider, IsEmailVerified, CreatedAt)
    VALUES (@AdminId, 'EPCL Super Admin', 'admin@epcl.in', '+919000000001',
            '$2a$11$x9ivXNzFc1JnlxdSZNh0e.0n9TENjgDtTf92xn1dMgMNcth6Ocm8q', 'SuperAdmin', 1, 0, 'Local', 1, GETUTCDATE());
    INSERT INTO UserProfiles (Id, UserId, PreferredLanguage)
    VALUES (NEWID(), @AdminId, 'en');
END
GO

-- Seed a Dealer user
USE EPCL_Identity;
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'dealer@epcl.in')
BEGIN
    DECLARE @DealerId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Users (Id, FullName, Email, PhoneNumber, PasswordHash, Role,
                       IsActive, FailedLoginAttempts, AuthProvider, IsEmailVerified, CreatedAt)
    VALUES (@DealerId, 'Test Dealer - Mumbai Central', 'dealer@epcl.in', '+919000000002',
            '$2a$11$x9ivXNzFc1JnlxdSZNh0e.0n9TENjgDtTf92xn1dMgMNcth6Ocm8q', 'Dealer', 1, 0, 'Local', 1, GETUTCDATE());
    INSERT INTO UserProfiles (Id, UserId, PreferredLanguage)
    VALUES (NEWID(), @DealerId, 'en');
END
GO

-- Seed a Customer user
USE EPCL_Identity;
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'customer@epcl.in')
BEGIN
    DECLARE @CustomerId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Users (Id, FullName, Email, PhoneNumber, PasswordHash, Role,
                       IsActive, FailedLoginAttempts, AuthProvider, IsEmailVerified, CreatedAt)
    VALUES (@CustomerId, 'Rahul Sharma', 'customer@epcl.in', '+919000000003',
            '$2a$11$x9ivXNzFc1JnlxdSZNh0e.0n9TENjgDtTf92xn1dMgMNcth6Ocm8q', 'Customer', 1, 0, 'Local', 1, GETUTCDATE());
    INSERT INTO UserProfiles (Id, UserId, PreferredLanguage)
    VALUES (NEWID(), @CustomerId, 'en');
END
GO

-- EPCL_Stations: Seed a test station
USE EPCL_Stations;
IF NOT EXISTS (SELECT 1 FROM Stations)
BEGIN
    DECLARE @StationId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Stations (Id, StationCode, StationName, DealerUserId, AddressLine1, 
                          City, State, PinCode, Latitude, Longitude, LicenseNumber,
                          OperatingHoursStart, OperatingHoursEnd, Is24x7, IsActive, CreatedAt)
    VALUES (NEWID(), 'MH-BNK-001', 'EPCL Mumbai Central', 
            (SELECT Id FROM EPCL_Identity.dbo.Users WHERE Email = 'dealer@epcl.in'),
            'Plot 14, Western Express Highway', 'Mumbai', 'Maharashtra', '400054',
            19.0760, 72.8777, 'MH-PET-2024-001234',
            '06:00', '22:00', 0, 1, GETUTCDATE());
END
GO

-- EPCL_Inventory: Seed tanks for the test station
USE EPCL_Inventory;
IF NOT EXISTS (SELECT 1 FROM Tanks)
BEGIN
    -- Petrol tank
    INSERT INTO Tanks (Id, StationId, FuelTypeId, TankSerialNumber, CapacityLitres, 
                       CurrentStockLitres, ReservedLitres, MinThresholdLitres, Status, CreatedAt)
    VALUES (NEWID(), 
            (SELECT TOP 1 Id FROM EPCL_Stations.dbo.Stations),
            (SELECT Id FROM EPCL_Stations.dbo.FuelTypes WHERE Name = 'Petrol'),
            'TK-MH001-P01', 15000.00, 8500.000, 0.000, 2000.00, 'Available', GETUTCDATE());

    -- Diesel tank
    INSERT INTO Tanks (Id, StationId, FuelTypeId, TankSerialNumber, CapacityLitres,
                       CurrentStockLitres, ReservedLitres, MinThresholdLitres, Status, CreatedAt)
    VALUES (NEWID(),
            (SELECT TOP 1 Id FROM EPCL_Stations.dbo.Stations),
            (SELECT Id FROM EPCL_Stations.dbo.FuelTypes WHERE Name = 'Diesel'),
            'TK-MH001-D01', 20000.00, 3000.000, 0.000, 2000.00, 'Low', GETUTCDATE());

    -- CNG tank  
    INSERT INTO Tanks (Id, StationId, FuelTypeId, TankSerialNumber, CapacityLitres,
                       CurrentStockLitres, ReservedLitres, MinThresholdLitres, Status, CreatedAt)
    VALUES (NEWID(),
            (SELECT TOP 1 Id FROM EPCL_Stations.dbo.Stations),
            (SELECT Id FROM EPCL_Stations.dbo.FuelTypes WHERE Name = 'CNG'),
            'TK-MH001-C01', 5000.00, 4200.000, 0.000, 500.00, 'Available', GETUTCDATE());
END
GO

-- EPCL_Sales: Seed pumps
USE EPCL_Sales;
IF NOT EXISTS (SELECT 1 FROM Pumps)
BEGIN
    -- Petrol pump 1
    INSERT INTO Pumps (Id, StationId, FuelTypeId, PumpName, NozzleCount, Status, CreatedAt)
    VALUES (NEWID(),
            (SELECT TOP 1 Id FROM EPCL_Stations.dbo.Stations),
            (SELECT Id FROM EPCL_Stations.dbo.FuelTypes WHERE Name = 'Petrol'),
            'Pump 1A', 2, 'Active', GETUTCDATE());

    -- Petrol pump 2
    INSERT INTO Pumps (Id, StationId, FuelTypeId, PumpName, NozzleCount, Status, CreatedAt)
    VALUES (NEWID(),
            (SELECT TOP 1 Id FROM EPCL_Stations.dbo.Stations),
            (SELECT Id FROM EPCL_Stations.dbo.FuelTypes WHERE Name = 'Petrol'),
            'Pump 1B', 2, 'Active', GETUTCDATE());

    -- Diesel pump
    INSERT INTO Pumps (Id, StationId, FuelTypeId, PumpName, NozzleCount, Status, CreatedAt)
    VALUES (NEWID(),
            (SELECT TOP 1 Id FROM EPCL_Stations.dbo.Stations),
            (SELECT Id FROM EPCL_Stations.dbo.FuelTypes WHERE Name = 'Diesel'),
            'Pump 2A', 2, 'Active', GETUTCDATE());

    -- CNG pump
    INSERT INTO Pumps (Id, StationId, FuelTypeId, PumpName, NozzleCount, Status, CreatedAt)
    VALUES (NEWID(),
            (SELECT TOP 1 Id FROM EPCL_Stations.dbo.Stations),
            (SELECT Id FROM EPCL_Stations.dbo.FuelTypes WHERE Name = 'CNG'),
            'Pump 3A', 1, 'UnderMaintenance', GETUTCDATE());
END
GO

-- EPCL_Sales: Seed current fuel prices
USE EPCL_Sales;
IF NOT EXISTS (SELECT 1 FROM FuelPrices)
BEGIN
    DECLARE @AdminUserId UNIQUEIDENTIFIER = 
        (SELECT Id FROM EPCL_Identity.dbo.Users WHERE Email = 'admin@epcl.in');
    
    INSERT INTO FuelPrices (Id, FuelTypeId, PricePerLitre, EffectiveFrom, IsActive, SetByUserId, CreatedAt)
    VALUES
    (NEWID(), (SELECT Id FROM EPCL_Stations.dbo.FuelTypes WHERE Name='Petrol'),
     96.720, GETUTCDATE(), 1, @AdminUserId, GETUTCDATE()),
    (NEWID(), (SELECT Id FROM EPCL_Stations.dbo.FuelTypes WHERE Name='Diesel'),
     89.620, GETUTCDATE(), 1, @AdminUserId, GETUTCDATE()),
    (NEWID(), (SELECT Id FROM EPCL_Stations.dbo.FuelTypes WHERE Name='CNG'),
     76.000, GETUTCDATE(), 1, @AdminUserId, GETUTCDATE()),
    (NEWID(), (SELECT Id FROM EPCL_Stations.dbo.FuelTypes WHERE Name='PremiumPetrol'),
     104.500, GETUTCDATE(), 1, @AdminUserId, GETUTCDATE()),
    (NEWID(), (SELECT Id FROM EPCL_Stations.dbo.FuelTypes WHERE Name='PremiumDiesel'),
     95.000, GETUTCDATE(), 1, @AdminUserId, GETUTCDATE());
END
GO

-- EPCL_Sales: Seed customer wallet (empty, ₹0 balance)
USE EPCL_Sales;
IF NOT EXISTS (SELECT 1 FROM CustomerWallets)
BEGIN
    INSERT INTO CustomerWallets (Id, CustomerId, Balance, TotalLoaded, IsActive, CreatedAt)
    VALUES (NEWID(),
            (SELECT Id FROM EPCL_Identity.dbo.Users WHERE Email = 'customer@epcl.in'),
            0.00, 0.00, 1, GETUTCDATE());
END
GO

-- EPCL_Loyalty: Seed loyalty accounts for customer
USE EPCL_Loyalty;
IF NOT EXISTS (SELECT 1 FROM LoyaltyAccounts)
BEGIN
    INSERT INTO LoyaltyAccounts (Id, CustomerId, PointsBalance, LifetimePoints, 
                                  Tier, CreatedAt)
    VALUES (NEWID(),
            (SELECT Id FROM EPCL_Identity.dbo.Users WHERE Email = 'customer@epcl.in'),
            0, 0, 'Silver', GETUTCDATE());
    
    -- Seed referral code
    INSERT INTO ReferralCodes (Id, CustomerId, Code, TotalReferrals, TotalPointsEarned, CreatedAt)
    VALUES (NEWID(),
            (SELECT Id FROM EPCL_Identity.dbo.Users WHERE Email = 'customer@epcl.in'),
            'RAHUL001', 0, 0, GETUTCDATE());
END
GO

-- Also update UserProfiles to set the StationId for the Dealer
USE EPCL_Identity;
UPDATE UserProfiles 
SET StationId = (SELECT TOP 1 Id FROM EPCL_Stations.dbo.Stations)
WHERE UserId = (SELECT Id FROM Users WHERE Email = 'dealer@epcl.in');
GO
