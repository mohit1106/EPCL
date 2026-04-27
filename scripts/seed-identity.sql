-- =====================================================
-- EPCL Identity Database Seeder
-- Adds 50 users (Admins, Dealers, Customers, Auditors)
-- Password for all new users: Password@123
-- BCrypt hash: $2a$11$x9ivXNzFc1JnlxdSZNh0e.0n9TENjgDtTf92xn1dMgMNcth6Ocm8q
-- =====================================================
USE EPCL_Identity;
GO

DECLARE @PwdHash NVARCHAR(200) = '$2a$11$x9ivXNzFc1JnlxdSZNh0e.0n9TENjgDtTf92xn1dMgMNcth6Ocm8q';
DECLARE @Now DATETIMEOFFSET = SYSDATETIMEOFFSET();

-- ── Additional Admins ──
INSERT INTO Users (Id, FullName, Email, PhoneNumber, PasswordHash, Role, IsActive, FailedLoginAttempts, CreatedAt, IsEmailVerified, AuthProvider)
SELECT * FROM (VALUES
('B0000001-0000-0000-0000-000000000001','Vikram Mehta','vikram.mehta@epcl.com','+91-9800000001',@PwdHash,'Admin',1,0,DATEADD(day,-80,@Now),1,'Local'),
('B0000001-0000-0000-0000-000000000002','Sunita Reddy','sunita.reddy@epcl.com','+91-9800000002',@PwdHash,'Admin',1,0,DATEADD(day,-75,@Now),1,'Local')
) AS V(Id,FullName,Email,PhoneNumber,PasswordHash,Role,IsActive,FailedLoginAttempts,CreatedAt,IsEmailVerified,AuthProvider)
WHERE NOT EXISTS (SELECT 1 FROM Users WHERE Email = V.Email);

-- ── Dealers (10 new) ──
INSERT INTO Users (Id, FullName, Email, PhoneNumber, PasswordHash, Role, IsActive, FailedLoginAttempts, CreatedAt, IsEmailVerified, AuthProvider)
SELECT * FROM (VALUES
('D0000001-0000-0000-0000-000000000001','Rajesh Patel','rajesh.patel@epcl.com','+91-9810000001',@PwdHash,'Dealer',1,0,DATEADD(day,-90,@Now),1,'Local'),
('D0000001-0000-0000-0000-000000000002','Arun Kumar','arun.kumar@epcl.com','+91-9810000002',@PwdHash,'Dealer',1,0,DATEADD(day,-88,@Now),1,'Local'),
('D0000001-0000-0000-0000-000000000003','Priya Sharma','priya.sharma@epcl.com','+91-9810000003',@PwdHash,'Dealer',1,0,DATEADD(day,-85,@Now),1,'Local'),
('D0000001-0000-0000-0000-000000000004','Suresh Nair','suresh.nair@epcl.com','+91-9810000004',@PwdHash,'Dealer',1,0,DATEADD(day,-82,@Now),1,'Local'),
('D0000001-0000-0000-0000-000000000005','Meena Iyer','meena.iyer@epcl.com','+91-9810000005',@PwdHash,'Dealer',1,0,DATEADD(day,-80,@Now),1,'Local'),
('D0000001-0000-0000-0000-000000000006','Ravi Deshmukh','ravi.deshmukh@epcl.com','+91-9810000006',@PwdHash,'Dealer',1,0,DATEADD(day,-78,@Now),1,'Local'),
('D0000001-0000-0000-0000-000000000007','Kavita Joshi','kavita.joshi@epcl.com','+91-9810000007',@PwdHash,'Dealer',1,0,DATEADD(day,-75,@Now),1,'Local'),
('D0000001-0000-0000-0000-000000000008','Amit Singh','amit.singh@epcl.com','+91-9810000008',@PwdHash,'Dealer',1,0,DATEADD(day,-72,@Now),1,'Local'),
('D0000001-0000-0000-0000-000000000009','Lakshmi Rao','lakshmi.rao@epcl.com','+91-9810000009',@PwdHash,'Dealer',1,0,DATEADD(day,-70,@Now),1,'Local'),
('D0000001-0000-0000-0000-000000000010','Deepak Gupta','deepak.gupta@epcl.com','+91-9810000010',@PwdHash,'Dealer',1,0,DATEADD(day,-68,@Now),1,'Local')
) AS V(Id,FullName,Email,PhoneNumber,PasswordHash,Role,IsActive,FailedLoginAttempts,CreatedAt,IsEmailVerified,AuthProvider)
WHERE NOT EXISTS (SELECT 1 FROM Users WHERE Email = V.Email);

-- ── Customers (30 new) ──
INSERT INTO Users (Id, FullName, Email, PhoneNumber, PasswordHash, Role, IsActive, FailedLoginAttempts, CreatedAt, IsEmailVerified, AuthProvider)
SELECT * FROM (VALUES
('C0000001-0000-0000-0000-000000000001','Rahul Sharma','rahul.sharma@gmail.com','+91-9820000001',@PwdHash,'Customer',1,0,DATEADD(day,-60,@Now),1,'Local'),
('C0000001-0000-0000-0000-000000000002','Anjali Verma','anjali.verma@gmail.com','+91-9820000002',@PwdHash,'Customer',1,0,DATEADD(day,-58,@Now),1,'Local'),
('C0000001-0000-0000-0000-000000000003','Sanjay Mishra','sanjay.mishra@gmail.com','+91-9820000003',@PwdHash,'Customer',1,0,DATEADD(day,-56,@Now),1,'Local'),
('C0000001-0000-0000-0000-000000000004','Neha Kapoor','neha.kapoor@gmail.com','+91-9820000004',@PwdHash,'Customer',1,0,DATEADD(day,-54,@Now),1,'Local'),
('C0000001-0000-0000-0000-000000000005','Rohit Malhotra','rohit.malhotra@gmail.com','+91-9820000005',@PwdHash,'Customer',1,0,DATEADD(day,-52,@Now),1,'Local'),
('C0000001-0000-0000-0000-000000000006','Divya Prasad','divya.prasad@gmail.com','+91-9820000006',@PwdHash,'Customer',1,0,DATEADD(day,-50,@Now),1,'Local'),
('C0000001-0000-0000-0000-000000000007','Manish Tiwari','manish.tiwari@gmail.com','+91-9820000007',@PwdHash,'Customer',1,0,DATEADD(day,-48,@Now),1,'Local'),
('C0000001-0000-0000-0000-000000000008','Pooja Agarwal','pooja.agarwal@gmail.com','+91-9820000008',@PwdHash,'Customer',1,0,DATEADD(day,-46,@Now),1,'Local'),
('C0000001-0000-0000-0000-000000000009','Vivek Chauhan','vivek.chauhan@gmail.com','+91-9820000009',@PwdHash,'Customer',1,0,DATEADD(day,-44,@Now),1,'Local'),
('C0000001-0000-0000-0000-000000000010','Sneha Kulkarni','sneha.kulkarni@gmail.com','+91-9820000010',@PwdHash,'Customer',1,0,DATEADD(day,-42,@Now),1,'Local'),
('C0000001-0000-0000-0000-000000000011','Arjun Pillai','arjun.pillai@gmail.com','+91-9820000011',@PwdHash,'Customer',1,0,DATEADD(day,-40,@Now),1,'Local'),
('C0000001-0000-0000-0000-000000000012','Rekha Saxena','rekha.saxena@gmail.com','+91-9820000012',@PwdHash,'Customer',1,0,DATEADD(day,-38,@Now),1,'Local'),
('C0000001-0000-0000-0000-000000000013','Karan Bhatia','karan.bhatia@gmail.com','+91-9820000013',@PwdHash,'Customer',1,0,DATEADD(day,-36,@Now),1,'Local'),
('C0000001-0000-0000-0000-000000000014','Shruti Menon','shruti.menon@gmail.com','+91-9820000014',@PwdHash,'Customer',1,0,DATEADD(day,-34,@Now),1,'Local'),
('C0000001-0000-0000-0000-000000000015','Gaurav Pandey','gaurav.pandey@gmail.com','+91-9820000015',@PwdHash,'Customer',1,0,DATEADD(day,-32,@Now),1,'Local'),
('C0000001-0000-0000-0000-000000000016','Nidhi Chatterjee','nidhi.chatterjee@gmail.com','+91-9820000016',@PwdHash,'Customer',1,0,DATEADD(day,-30,@Now),1,'Local'),
('C0000001-0000-0000-0000-000000000017','Abhishek Das','abhishek.das@gmail.com','+91-9820000017',@PwdHash,'Customer',1,0,DATEADD(day,-28,@Now),1,'Local'),
('C0000001-0000-0000-0000-000000000018','Ritu Bansal','ritu.bansal@gmail.com','+91-9820000018',@PwdHash,'Customer',1,0,DATEADD(day,-26,@Now),1,'Local'),
('C0000001-0000-0000-0000-000000000019','Nikhil Hegde','nikhil.hegde@gmail.com','+91-9820000019',@PwdHash,'Customer',1,0,DATEADD(day,-24,@Now),1,'Local'),
('C0000001-0000-0000-0000-000000000020','Pallavi Sinha','pallavi.sinha@gmail.com','+91-9820000020',@PwdHash,'Customer',1,0,DATEADD(day,-22,@Now),1,'Local'),
('C0000001-0000-0000-0000-000000000021','Tarun Bhatt','tarun.bhatt@gmail.com','+91-9820000021',@PwdHash,'Customer',1,0,DATEADD(day,-20,@Now),1,'Local'),
('C0000001-0000-0000-0000-000000000022','Swati Gokhale','swati.gokhale@gmail.com','+91-9820000022',@PwdHash,'Customer',1,0,DATEADD(day,-18,@Now),1,'Local'),
('C0000001-0000-0000-0000-000000000023','Ashish Dutta','ashish.dutta@gmail.com','+91-9820000023',@PwdHash,'Customer',1,0,DATEADD(day,-16,@Now),1,'Local'),
('C0000001-0000-0000-0000-000000000024','Madhavi Rajan','madhavi.rajan@gmail.com','+91-9820000024',@PwdHash,'Customer',1,0,DATEADD(day,-14,@Now),1,'Local'),
('C0000001-0000-0000-0000-000000000025','Pranav Thakur','pranav.thakur@gmail.com','+91-9820000025',@PwdHash,'Customer',1,0,DATEADD(day,-12,@Now),1,'Local'),
('C0000001-0000-0000-0000-000000000026','Ishita Ghosh','ishita.ghosh@gmail.com','+91-9820000026',@PwdHash,'Customer',1,0,DATEADD(day,-10,@Now),1,'Local'),
('C0000001-0000-0000-0000-000000000027','Varun Sethi','varun.sethi@gmail.com','+91-9820000027',@PwdHash,'Customer',1,0,DATEADD(day,-8,@Now),1,'Local'),
('C0000001-0000-0000-0000-000000000028','Tanvi Jain','tanvi.jain@gmail.com','+91-9820000028',@PwdHash,'Customer',1,0,DATEADD(day,-6,@Now),1,'Local'),
('C0000001-0000-0000-0000-000000000029','Mohit Saxena','mohit.saxena@gmail.com','+91-9820000029',@PwdHash,'Customer',1,0,DATEADD(day,-4,@Now),1,'Local'),
('C0000001-0000-0000-0000-000000000030','Aditi Mukherjee','aditi.mukherjee@gmail.com','+91-9820000030',@PwdHash,'Customer',1,0,DATEADD(day,-2,@Now),1,'Local')
) AS V(Id,FullName,Email,PhoneNumber,PasswordHash,Role,IsActive,FailedLoginAttempts,CreatedAt,IsEmailVerified,AuthProvider)
WHERE NOT EXISTS (SELECT 1 FROM Users WHERE Email = V.Email);

-- ── Auditors (2) ──
INSERT INTO Users (Id, FullName, Email, PhoneNumber, PasswordHash, Role, IsActive, FailedLoginAttempts, CreatedAt, IsEmailVerified, AuthProvider)
SELECT * FROM (VALUES
('A0000001-0000-0000-0000-000000000001','Ramesh Auditor','ramesh.auditor@epcl.com','+91-9830000001',@PwdHash,'Auditor',1,0,DATEADD(day,-70,@Now),1,'Local'),
('A0000001-0000-0000-0000-000000000002','Savita Auditor','savita.auditor@epcl.com','+91-9830000002',@PwdHash,'Auditor',1,0,DATEADD(day,-65,@Now),1,'Local')
) AS V(Id,FullName,Email,PhoneNumber,PasswordHash,Role,IsActive,FailedLoginAttempts,CreatedAt,IsEmailVerified,AuthProvider)
WHERE NOT EXISTS (SELECT 1 FROM Users WHERE Email = V.Email);

PRINT 'EPCL_Identity seeded successfully.';
GO
