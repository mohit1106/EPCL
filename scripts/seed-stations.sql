USE EPCL_Stations;
GO

DECLARE @Now DATETIMEOFFSET = SYSDATETIMEOFFSET();

IF NOT EXISTS (SELECT 1 FROM Stations WHERE StationCode = 'EPCL-MUM-001')
BEGIN
INSERT INTO Stations (Id, StationCode, StationName, DealerUserId, AddressLine1, City, State, PinCode, Latitude, Longitude, LicenseNumber, OperatingHoursStart, OperatingHoursEnd, Is24x7, IsActive, CreatedAt) VALUES
(NEWID(),'EPCL-MUM-001','EPCL Andheri West','D0000001-0000-0000-0000-000000000001','Plot 45, SV Road, Andheri West','Mumbai','Maharashtra','400058',19.136399,72.829735,'MH-LIC-2024-001','06:00','23:00',0,1,DATEADD(day,-90,@Now)),
(NEWID(),'EPCL-MUM-002','EPCL Bandra East','D0000001-0000-0000-0000-000000000002','23 Hill Road, Bandra East','Mumbai','Maharashtra','400051',19.054080,72.840340,'MH-LIC-2024-002','00:00','23:59',1,1,DATEADD(day,-88,@Now)),
(NEWID(),'EPCL-MUM-003','EPCL Powai','D0000001-0000-0000-0000-000000000003','Hiranandani Gardens, Powai','Mumbai','Maharashtra','400076',19.117330,72.905640,'MH-LIC-2024-003','06:00','22:00',0,1,DATEADD(day,-85,@Now)),
(NEWID(),'EPCL-DEL-001','EPCL Connaught Place','D0000001-0000-0000-0000-000000000004','Block C, Connaught Place','New Delhi','Delhi','110001',28.632736,77.219574,'DL-LIC-2024-001','00:00','23:59',1,1,DATEADD(day,-82,@Now)),
(NEWID(),'EPCL-DEL-002','EPCL Dwarka Sec-12','D0000001-0000-0000-0000-000000000005','Sector 12, Main Road, Dwarka','New Delhi','Delhi','110075',28.591750,77.048570,'DL-LIC-2024-002','05:30','23:30',0,1,DATEADD(day,-80,@Now)),
(NEWID(),'EPCL-DEL-003','EPCL Rohini','D0000001-0000-0000-0000-000000000006','Plot 78, Rohini Sector 3','New Delhi','Delhi','110085',28.715820,77.106780,'DL-LIC-2024-003','06:00','22:00',0,1,DATEADD(day,-78,@Now)),
(NEWID(),'EPCL-CHN-001','EPCL T.Nagar','D0000001-0000-0000-0000-000000000007','Usman Road, T.Nagar','Chennai','Tamil Nadu','600017',13.040870,80.233950,'TN-LIC-2024-001','00:00','23:59',1,1,DATEADD(day,-75,@Now)),
(NEWID(),'EPCL-CHN-002','EPCL OMR Sholinganallur','D0000001-0000-0000-0000-000000000008','OMR Road, Sholinganallur','Chennai','Tamil Nadu','600119',12.901150,80.227600,'TN-LIC-2024-002','06:00','23:00',0,1,DATEADD(day,-72,@Now)),
(NEWID(),'EPCL-HYD-001','EPCL Hitech City','D0000001-0000-0000-0000-000000000009','Madhapur, Hitech City','Hyderabad','Telangana','500081',17.449760,78.381340,'TS-LIC-2024-001','00:00','23:59',1,1,DATEADD(day,-70,@Now)),
(NEWID(),'EPCL-HYD-002','EPCL Gachibowli','D0000001-0000-0000-0000-000000000010','Financial District, Gachibowli','Hyderabad','Telangana','500032',17.440400,78.348680,'TS-LIC-2024-002','06:00','22:30',0,1,DATEADD(day,-68,@Now)),
(NEWID(),'EPCL-BLR-001','EPCL Koramangala','D0000001-0000-0000-0000-000000000001','80 Feet Road, Koramangala','Bangalore','Karnataka','560034',12.934790,77.623420,'KA-LIC-2024-001','00:00','23:59',1,1,DATEADD(day,-65,@Now)),
(NEWID(),'EPCL-BLR-002','EPCL Whitefield','D0000001-0000-0000-0000-000000000002','ITPL Road, Whitefield','Bangalore','Karnataka','560066',12.969660,77.749720,'KA-LIC-2024-002','06:00','23:00',0,1,DATEADD(day,-62,@Now)),
(NEWID(),'EPCL-PUN-001','EPCL Hinjewadi','D0000001-0000-0000-0000-000000000003','Phase 1, Hinjewadi IT Park','Pune','Maharashtra','411057',18.591280,73.738600,'MH-LIC-2024-004','06:00','22:00',0,1,DATEADD(day,-58,@Now)),
(NEWID(),'EPCL-KOL-001','EPCL Salt Lake','D0000001-0000-0000-0000-000000000004','Sector V, Salt Lake','Kolkata','West Bengal','700091',22.576880,88.432520,'WB-LIC-2024-001','05:30','22:30',0,1,DATEADD(day,-55,@Now)),
(NEWID(),'EPCL-AHM-001','EPCL SG Highway','D0000001-0000-0000-0000-000000000005','SG Highway, Bodakdev','Ahmedabad','Gujarat','380054',23.039570,72.511600,'GJ-LIC-2024-001','00:00','23:59',1,1,DATEADD(day,-50,@Now));
END;

PRINT 'EPCL_Stations seeded successfully.';
SELECT COUNT(*) AS StationCount FROM Stations;
GO
