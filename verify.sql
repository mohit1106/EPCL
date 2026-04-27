SET NOCOUNT ON;
SELECT 'FuelTypes' AS TableName, COUNT(*) AS Cnt FROM EPCL_Stations.dbo.FuelTypes UNION ALL
SELECT 'Stations', COUNT(*) FROM EPCL_Stations.dbo.Stations UNION ALL
SELECT 'Tanks', COUNT(*) FROM EPCL_Inventory.dbo.Tanks UNION ALL
SELECT 'Pumps', COUNT(*) FROM EPCL_Sales.dbo.Pumps UNION ALL
SELECT 'FuelPrices', COUNT(*) FROM EPCL_Sales.dbo.FuelPrices UNION ALL
SELECT 'Users', COUNT(*) FROM EPCL_Identity.dbo.Users UNION ALL
SELECT 'LoyaltyAccounts', COUNT(*) FROM EPCL_Loyalty.dbo.LoyaltyAccounts;
GO
