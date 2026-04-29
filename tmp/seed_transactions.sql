-- Seed 50 additional transactions for customer@epcl.in (Rahul Sharma)
-- CustomerUserId: 0CA48355-CCDD-49C6-B768-BFCDDFEE712C

DECLARE @CustId uniqueidentifier = '0CA48355-CCDD-49C6-B768-BFCDDFEE712C';
DECLARE @DealerId uniqueidentifier = 'D0000001-0000-0000-0000-000000000001';
DECLARE @i INT = 1;

-- Station/Pump/FuelType pools
DECLARE @Stations TABLE(Id uniqueidentifier);
INSERT @Stations VALUES 
('513F1815-BC52-4333-AFFA-58DF0723EB2F'),('E6C93ACC-5A03-4180-A5D5-08BFA8486768'),
('D8AF7C6F-F357-44FE-AFA7-3CA2472E7D6B'),('430A8996-ED9D-4F90-8D94-2B1671126575'),
('97A2D6CF-0599-4992-A288-A44E1E58E399'),('EE336B27-C368-4727-AA76-F9E4CE5D44AC');

DECLARE @Pumps TABLE(Id uniqueidentifier);
INSERT @Pumps VALUES 
('718FF0BF-D5BD-4D83-B373-6E0828C0B240'),('1B5DDF78-1E82-41B7-BE19-09B84AC67A95'),
('ED89C2DB-F124-4EAD-BEF4-171DEEFA5724'),('F2066A00-0113-46B5-AE29-19264F034D96'),
('C94AEA3E-ED37-4558-AB76-19FC1B53F2B4'),('6FEB615C-8098-413D-978E-1E61FE08BBAD');

DECLARE @FuelTypes TABLE(Id uniqueidentifier, Price DECIMAL(10,2));
INSERT @FuelTypes VALUES 
('A1B2C3D4-E5F6-7890-ABCD-000000000001', 96.72),  -- Petrol
('A1B2C3D4-E5F6-7890-ABCD-000000000002', 89.62),  -- Diesel
('A1B2C3D4-E5F6-7890-ABCD-000000000003', 76.00),  -- CNG
('A1B2C3D4-E5F6-7890-ABCD-000000000004', 104.50), -- PremiumPetrol
('A1B2C3D4-E5F6-7890-ABCD-000000000005', 95.00);  -- PremiumDiesel

DECLARE @Vehicles TABLE(Num VARCHAR(15));
INSERT @Vehicles VALUES ('MH-02-CD-5678'),('MH-01-AB-1234'),('TN-01-IJ-7890'),('MH-03-YZ-0123'),('DL-02-ST-8901'),('KA-03-CD-8901'),('DL-03-AB-4567');

DECLARE @Payments TABLE(Method NVARCHAR(20));
INSERT @Payments VALUES ('Cash'),('UPI'),('Card'),('Wallet');

WHILE @i <= 50
BEGIN
  DECLARE @StationId uniqueidentifier = (SELECT TOP 1 Id FROM @Stations ORDER BY NEWID());
  DECLARE @PumpId uniqueidentifier = (SELECT TOP 1 Id FROM @Pumps ORDER BY NEWID());
  DECLARE @FuelTypeId uniqueidentifier;
  DECLARE @FuelPrice DECIMAL(10,2);
  SELECT TOP 1 @FuelTypeId = Id, @FuelPrice = Price FROM @FuelTypes ORDER BY NEWID();
  DECLARE @Vehicle VARCHAR(15) = (SELECT TOP 1 Num FROM @Vehicles ORDER BY NEWID());
  DECLARE @Payment NVARCHAR(20) = (SELECT TOP 1 Method FROM @Payments ORDER BY NEWID());
  DECLARE @Qty DECIMAL(10,3) = CAST(5 + RAND() * 45 AS DECIMAL(10,1));
  DECLARE @Total DECIMAL(10,2) = @Qty * @FuelPrice;
  DECLARE @TxDate DATETIMEOFFSET = DATEADD(DAY, -@i, GETDATE());
  DECLARE @Status NVARCHAR(20) = CASE WHEN @i % 15 = 0 THEN 'Voided' WHEN @i % 10 = 0 THEN 'Initiated' ELSE 'Completed' END;
  DECLARE @ReceiptNum VARCHAR(20) = 'RCP-' + RIGHT('000000' + CAST(1000 + @i AS VARCHAR(6)), 6);

  INSERT INTO Transactions (Id, ReceiptNumber, StationId, PumpId, FuelTypeId, DealerUserId, CustomerUserId, VehicleNumber,
    QuantityLitres, PricePerLitre, TotalAmount, PaymentMethod, PaymentReferenceId, Status, FraudCheckStatus,
    LoyaltyPointsEarned, LoyaltyPointsRedeemed, Timestamp, IsVoided)
  VALUES (NEWID(), @ReceiptNum, @StationId, @PumpId, @FuelTypeId, @DealerId, @CustId, @Vehicle,
    @Qty, @FuelPrice, @Total, @Payment, NULL, @Status, 'Cleared',
    CAST(@Total / 100 AS INT), 0, @TxDate, CASE WHEN @Status = 'Voided' THEN 1 ELSE 0 END);

  SET @i = @i + 1;
END;

SELECT COUNT(*) AS TotalCustomerTxns FROM Transactions WHERE CustomerUserId = @CustId;
GO
