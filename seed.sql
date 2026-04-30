SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

USE EPCL_Stations;

IF NOT EXISTS (SELECT 1 FROM FuelTypes)
BEGIN
    INSERT INTO FuelTypes (Id, Name, Description, IsActive)
    VALUES
      (NEWID(), 'Petrol',         'Regular unleaded petrol',           1),
      (NEWID(), 'Diesel',         'High-speed diesel',                 1),
      (NEWID(), 'CNG',            'Compressed Natural Gas',            1),
      (NEWID(), 'PremiumPetrol',  'High-octane premium petrol (97+)',  1),
      (NEWID(), 'PremiumDiesel',  'Premium diesel with additives',     1);
END
GO

USE EPCL_Identity;

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

IF NOT EXISTS (SELECT 1 FROM Users WHERE Role = 'SuperAdmin')
BEGIN
    DECLARE @UserId UNIQUEIDENTIFIER = NEWID();

    INSERT INTO Users (Id, FullName, Email, PhoneNumber, PasswordHash, Role, IsActive, 
                       FailedLoginAttempts, CreatedAt, IsEmailVerified)
    VALUES (
      @UserId,
      'EPCL Super Admin',
      'admin@epcl.in',
      '+919999999999',
      '$2a$11$Zo3hPtBX6KRoa3LdA0Bdi.0KHst1qel0Esk.SqqqaSgTxvjg31Rfm',
      'SuperAdmin',
      1,
      0,
      GETUTCDATE(),
      1
    );

    INSERT INTO UserProfiles (Id, UserId, PreferredLanguage)
    VALUES (NEWID(), @UserId, 'en');
END
GO