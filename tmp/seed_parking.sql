-- Seed parking slots for all 16 EPCL stations
-- Mix of TwoWheeler, FourWheeler, and HGV slots per station

DECLARE @Stations TABLE(Id UNIQUEIDENTIFIER, StationName VARCHAR(50));
INSERT @Stations
SELECT Id, StationName FROM EPCL_Stations.dbo.Stations;

DECLARE @sid UNIQUEIDENTIFIER;
DECLARE @sname VARCHAR(50);
DECLARE @i INT;

DECLARE cur CURSOR FOR SELECT Id, StationName FROM @Stations;
OPEN cur;
FETCH NEXT FROM cur INTO @sid, @sname;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- 6 TwoWheeler slots
    SET @i = 1;
    WHILE @i <= 6
    BEGIN
        INSERT INTO ParkingSlots (Id, StationId, SlotType, SlotNumber, IsAvailable)
        VALUES (NEWID(), @sid, 'TwoWheeler', CONCAT('2W-', RIGHT('0' + CAST(@i AS VARCHAR), 2)), 1);
        SET @i = @i + 1;
    END;

    -- 8 FourWheeler slots
    SET @i = 1;
    WHILE @i <= 8
    BEGIN
        INSERT INTO ParkingSlots (Id, StationId, SlotType, SlotNumber, IsAvailable)
        VALUES (NEWID(), @sid, 'FourWheeler', CONCAT('4W-', RIGHT('0' + CAST(@i AS VARCHAR), 2)), 1);
        SET @i = @i + 1;
    END;

    -- 3 HGV slots
    SET @i = 1;
    WHILE @i <= 3
    BEGIN
        INSERT INTO ParkingSlots (Id, StationId, SlotType, SlotNumber, IsAvailable)
        VALUES (NEWID(), @sid, 'HGV', CONCAT('HGV-', RIGHT('0' + CAST(@i AS VARCHAR), 2)), 1);
        SET @i = @i + 1;
    END;

    FETCH NEXT FROM cur INTO @sid, @sname;
END;

CLOSE cur;
DEALLOCATE cur;

SELECT SlotType, COUNT(*) AS TotalSlots FROM ParkingSlots GROUP BY SlotType;
SELECT COUNT(DISTINCT StationId) AS StationsWithParking FROM ParkingSlots;
GO
