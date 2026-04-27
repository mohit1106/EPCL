# EPCL Master Seeder - Reads station IDs dynamically and seeds all databases
$server = "localhost\SQLEXPRESS"
$ErrorActionPreference = "Stop"

function Run-Sql($db, $sql) { Invoke-Sqlcmd -ServerInstance $server -Database $db -Query $sql }

Write-Host "=== EPCL Database Seeder ===" -ForegroundColor Cyan

# ── 1. Clear existing seed data from Inventory (has FK issues with old data) ──
Write-Host "Clearing old inventory seed data..." -ForegroundColor Yellow
Run-Sql "EPCL_Inventory" "DELETE FROM DipReadings; DELETE FROM StockLoadings; DELETE FROM ReplenishmentRequests; DELETE FROM Tanks WHERE TankSerialNumber LIKE 'TNK-%';"

# ── 2. Get station IDs ──
$stations = Run-Sql "EPCL_Stations" "SELECT Id, StationCode, DealerUserId FROM Stations WHERE StationCode LIKE 'EPCL-%' ORDER BY StationCode"
Write-Host "  Found $($stations.Count) stations" -ForegroundColor Green

$petrolId = "a1b2c3d4-e5f6-7890-abcd-000000000001"
$dieselId = "a1b2c3d4-e5f6-7890-abcd-000000000002"
$cngId    = "a1b2c3d4-e5f6-7890-abcd-000000000003"

# ── 3. Seed Tanks (3 per station = 45 tanks) ──
Write-Host "Seeding tanks..." -ForegroundColor Yellow
$tankIds = @()
foreach ($s in $stations) {
    $sid = $s.Id.ToString()
    $did = $s.DealerUserId.ToString()
    $code = $s.StationCode
    $fuels = @(
        @{fid=$petrolId; ser="TNK-P-$code"; cap=20000; stock=(12000 + (Get-Random -Max 6000)); min=3000},
        @{fid=$dieselId; ser="TNK-D-$code"; cap=15000; stock=(8000 + (Get-Random -Max 5000)); min=2500},
        @{fid=$cngId;    ser="TNK-C-$code"; cap=10000; stock=(4000 + (Get-Random -Max 4000)); min=1500}
    )
    foreach ($f in $fuels) {
        $tid = [Guid]::NewGuid().ToString()
        $tankIds += @{TankId=$tid; StationId=$sid; DealerId=$did; FuelTypeId=$f.fid}
        $daysAgo = Get-Random -Min 1 -Max 7
        Run-Sql "EPCL_Inventory" "INSERT INTO Tanks (Id,StationId,FuelTypeId,TankSerialNumber,CapacityLitres,CurrentStockLitres,ReservedLitres,MinThresholdLitres,Status,LastReplenishedAt,LastDipReadingAt,CreatedAt) VALUES ('$tid','$sid','$($f.fid)','$($f.ser)',$($f.cap),$($f.stock),0,$($f.min),0,DATEADD(day,-$daysAgo,SYSDATETIMEOFFSET()),DATEADD(hour,-$(Get-Random -Max 12),SYSDATETIMEOFFSET()),DATEADD(day,-90,SYSDATETIMEOFFSET()))"
    }
}
# Set some tanks to Low/Critical
Run-Sql "EPCL_Inventory" "UPDATE TOP(3) Tanks SET Status=1, CurrentStockLitres=MinThresholdLitres*0.8"
Run-Sql "EPCL_Inventory" "UPDATE TOP(2) Tanks SET Status=2, CurrentStockLitres=MinThresholdLitres*0.3 WHERE Status=0"
Write-Host "  $($tankIds.Count) tanks created" -ForegroundColor Green

# ── 4. Seed Stock Loadings (100) ──
Write-Host "Seeding stock loadings..." -ForegroundColor Yellow
$suppliers = @("Indian Oil Corp","BPCL Logistics","HPCL Supply Chain","Reliance Fuel Depot","Nayara Energy")
for ($i=1; $i -le 100; $i++) {
    $t = $tankIds | Get-Random
    $qty = 5000 + (Get-Random -Max 7000)
    $sup = $suppliers | Get-Random
    $days = Get-Random -Max 90
    Run-Sql "EPCL_Inventory" "INSERT INTO StockLoadings (Id,TankId,QuantityLoadedLitres,LoadedByUserId,TankerNumber,InvoiceNumber,SupplierName,StockBefore,StockAfter,Timestamp) VALUES (NEWID(),'$($t.TankId)',$qty,'$($t.DealerId)','TKR-$('{0:D4}' -f $i)','INV-2024-$('{0:D4}' -f $i)','$sup',$(Get-Random -Min 3000 -Max 8000),$(Get-Random -Min 10000 -Max 18000),DATEADD(day,-$days,SYSDATETIMEOFFSET()))"
}
Write-Host "  100 stock loadings created" -ForegroundColor Green

# ── 5. Seed Dip Readings (150) ──
Write-Host "Seeding dip readings..." -ForegroundColor Yellow
for ($i=1; $i -le 150; $i++) {
    $t = $tankIds | Get-Random
    $sysStock = Get-Random -Min 5000 -Max 15000
    $variance = [math]::Round((Get-Random -Min -200 -Max 200), 2)
    $dipVal = $sysStock + $variance
    $varPct = [math]::Round($variance / $sysStock * 100, 2)
    $flagged = if ([math]::Abs($varPct) -gt 2) { 1 } else { 0 }
    Run-Sql "EPCL_Inventory" "INSERT INTO DipReadings (Id,TankId,DipValueLitres,SystemStockLitres,VarianceLitres,VariancePercent,IsFraudFlagged,RecordedByUserId,Timestamp) VALUES (NEWID(),'$($t.TankId)',$dipVal,$sysStock,$variance,$varPct,$flagged,'$($t.DealerId)',DATEADD(hour,-$($i*8),SYSDATETIMEOFFSET()))"
}
Write-Host "  150 dip readings created" -ForegroundColor Green

# ── 6. Seed Sales: Pumps (60), FuelPrices (5) ──
Write-Host "Seeding pumps and fuel prices..." -ForegroundColor Yellow
Run-Sql "EPCL_Sales" "IF NOT EXISTS (SELECT 1 FROM FuelPrices WHERE IsActive=1) BEGIN
INSERT INTO FuelPrices (Id,FuelTypeId,PricePerLitre,EffectiveFrom,IsActive,SetByUserId,CreatedAt) VALUES
(NEWID(),'$petrolId',104.50,DATEADD(day,-30,SYSDATETIMEOFFSET()),1,'B0000001-0000-0000-0000-000000000001',DATEADD(day,-30,SYSDATETIMEOFFSET())),
(NEWID(),'$dieselId',89.62,DATEADD(day,-30,SYSDATETIMEOFFSET()),1,'B0000001-0000-0000-0000-000000000001',DATEADD(day,-30,SYSDATETIMEOFFSET())),
(NEWID(),'$cngId',76.00,DATEADD(day,-30,SYSDATETIMEOFFSET()),1,'B0000001-0000-0000-0000-000000000001',DATEADD(day,-30,SYSDATETIMEOFFSET())),
(NEWID(),'a1b2c3d4-e5f6-7890-abcd-000000000004',114.90,DATEADD(day,-30,SYSDATETIMEOFFSET()),1,'B0000001-0000-0000-0000-000000000001',DATEADD(day,-30,SYSDATETIMEOFFSET())),
(NEWID(),'a1b2c3d4-e5f6-7890-abcd-000000000005',96.72,DATEADD(day,-30,SYSDATETIMEOFFSET()),1,'B0000001-0000-0000-0000-000000000001',DATEADD(day,-30,SYSDATETIMEOFFSET()))
END"

$pumpIds = @()
foreach ($s in $stations) {
    $sid = $s.Id.ToString()
    $fuelMap = @($petrolId,$petrolId,$dieselId,$cngId)
    $pNames = @("Pump-01","Pump-02","Pump-03","Pump-04")
    for ($p=0; $p -lt 4; $p++) {
        $pumpGuid = [Guid]::NewGuid().ToString()
        $pumpIds += @{PumpId=$pumpGuid; StationId=$sid; FuelTypeId=$fuelMap[$p]; DealerId=$s.DealerUserId.ToString()}
        Run-Sql "EPCL_Sales" "IF NOT EXISTS (SELECT 1 FROM Pumps WHERE StationId='$sid' AND PumpName='$($pNames[$p])') INSERT INTO Pumps (Id,StationId,FuelTypeId,PumpName,NozzleCount,Status,CreatedAt) VALUES ('$pumpGuid','$sid','$($fuelMap[$p])','$($pNames[$p])',2,0,DATEADD(day,-90,SYSDATETIMEOFFSET()))"
    }
}
Write-Host "  $($pumpIds.Count) pumps, 5 fuel prices created" -ForegroundColor Green

# ── 7. Seed Transactions (500 - keeping reasonable for speed) ──
Write-Host "Seeding transactions (500)..." -ForegroundColor Yellow
$customerIds = (Run-Sql "EPCL_Identity" "SELECT CAST(Id AS VARCHAR(36)) AS Id FROM Users WHERE Role='Customer'") | ForEach-Object { $_.Id }
$vehicles = @("MH-01-AB-1234","MH-02-CD-5678","DL-01-EF-9012","KA-01-GH-3456","TN-01-IJ-7890","TS-01-KL-2345","GJ-01-MN-6789","WB-01-OP-0123","MH-04-QR-4567","DL-02-ST-8901","KA-02-UV-2345","TN-02-WX-6789","MH-03-YZ-0123","DL-03-AB-4567","KA-03-CD-8901")
$payments = @(0,1,2,4) # Cash,UPI,Card,Wallet

for ($i=1; $i -le 500; $i++) {
    $pump = $pumpIds | Get-Random
    $cust = $customerIds | Get-Random
    $veh = $vehicles | Get-Random
    $qty = [math]::Round((5 + (Get-Random -Max 45)), 2)
    $price = switch ($pump.FuelTypeId) { $petrolId {104.50} $dieselId {89.62} $cngId {76.00} default {104.50} }
    $total = [math]::Round($qty * $price, 2)
    $pay = $payments | Get-Random
    $days = Get-Random -Max 90
    $hrs = Get-Random -Max 24
    $receipt = "RCP-$('{0:D6}' -f $i)"
    $loyPts = [math]::Floor($total / 100)
    Run-Sql "EPCL_Sales" "INSERT INTO Transactions (Id,ReceiptNumber,StationId,PumpId,FuelTypeId,DealerUserId,CustomerUserId,VehicleNumber,QuantityLitres,PricePerLitre,TotalAmount,PaymentMethod,Status,FraudCheckStatus,LoyaltyPointsEarned,LoyaltyPointsRedeemed,Timestamp,IsVoided) VALUES (NEWID(),'$receipt','$($pump.StationId)','$($pump.PumpId)','$($pump.FuelTypeId)','$($pump.DealerId)','$cust','$veh',$qty,$price,$total,$pay,2,1,$loyPts,0,DATEADD(hour,-$(($days*24)+$hrs),SYSDATETIMEOFFSET()),0)"
    if ($i % 100 -eq 0) { Write-Host "    $i/500..." }
}
Write-Host "  500 transactions created" -ForegroundColor Green

# ── 8. Seed Customer Wallets (30) ──
Write-Host "Seeding wallets..." -ForegroundColor Yellow
foreach ($cid in $customerIds) {
    $bal = Get-Random -Min 500 -Max 15000
    $loaded = $bal + (Get-Random -Min 5000 -Max 30000)
    Run-Sql "EPCL_Sales" "IF NOT EXISTS (SELECT 1 FROM CustomerWallets WHERE CustomerId='$cid') INSERT INTO CustomerWallets (Id,CustomerId,Balance,TotalLoaded,IsActive,CreatedAt) VALUES (NEWID(),'$cid',$bal,$loaded,1,DATEADD(day,-60,SYSDATETIMEOFFSET()))"
}
Write-Host "  $($customerIds.Count) wallets created" -ForegroundColor Green

# ── 9. Seed Vehicles (15) ──
Write-Host "Seeding vehicles..." -ForegroundColor Yellow
$vTypes = @(0,1,1,1,2,3) # TwoWheeler,FourWheeler,Commercial,CNG
for ($i=0; $i -lt 15; $i++) {
    $cid = $customerIds[$i % $customerIds.Count]
    $vt = $vTypes | Get-Random
    Run-Sql "EPCL_Sales" "IF NOT EXISTS (SELECT 1 FROM RegisteredVehicles WHERE RegistrationNumber='$($vehicles[$i])') INSERT INTO RegisteredVehicles (Id,CustomerId,RegistrationNumber,VehicleType,Nickname,IsActive,RegisteredAt) VALUES (NEWID(),'$cid','$($vehicles[$i])',$vt,'My Vehicle $($i+1)',1,DATEADD(day,-$(Get-Random -Max 60),SYSDATETIMEOFFSET()))"
}
Write-Host "  15 vehicles created" -ForegroundColor Green

# ── 10. Seed Fraud Alerts (100) ──
Write-Host "Seeding fraud alerts..." -ForegroundColor Yellow
$txnIds = (Run-Sql "EPCL_Sales" "SELECT TOP 100 CAST(Id AS VARCHAR(36)) AS Id, CAST(StationId AS VARCHAR(36)) AS StationId FROM Transactions ORDER BY NEWID()") 
$rules = @("HighVolumeRule","PriceAnomalyRule","OffHoursRule","VelocityRule","DuplicateReceiptRule")
$severities = @(0,0,1,1,1,2,2) # Low,Medium,High
$statuses = @(0,0,0,0,1,1,1,2,2,3) # Open,UnderReview,Dismissed,Escalated
foreach ($tx in $txnIds) {
    $rule = $rules | Get-Random
    $sev = $severities | Get-Random
    $stat = $statuses | Get-Random
    Run-Sql "EPCL_Fraud" "INSERT INTO FraudAlerts (Id,TransactionId,StationId,RuleTriggered,Severity,Description,Status,CreatedAt) VALUES (NEWID(),'$($tx.Id)','$($tx.StationId)','$rule',$sev,'Auto-detected by $rule engine',$stat,DATEADD(day,-$(Get-Random -Max 90),SYSDATETIMEOFFSET()))"
}
Write-Host "  $($txnIds.Count) fraud alerts created" -ForegroundColor Green

# ── 11. Seed Audit Logs (200) ──
Write-Host "Seeding audit logs..." -ForegroundColor Yellow
$entityTypes = @("Transaction","Station","Tank","User","FuelPrice","Shift","StockLoading")
$operations = @("Create","Create","Create","Update","Update")
$services = @("SalesService","StationService","InventoryService","IdentityService","ReportingService")
for ($i=1; $i -le 200; $i++) {
    $et = $entityTypes | Get-Random
    $op = $operations | Get-Random
    $svc = $services | Get-Random
    Run-Sql "EPCL_Audit" "INSERT INTO AuditLogs (Id,EventId,EntityType,EntityId,Operation,NewValues,ChangedByUserId,ChangedByRole,ServiceName,Timestamp) VALUES (NEWID(),NEWID(),'$et',NEWID(),'$op','{""status"":""completed""}','B0000001-0000-0000-0000-000000000001','Admin','$svc',DATEADD(hour,-$($i*4),SYSDATETIMEOFFSET()))"
}
Write-Host "  200 audit logs created" -ForegroundColor Green

# ── 12. Seed Loyalty (accounts + transactions) ──
Write-Host "Seeding loyalty..." -ForegroundColor Yellow
$tiers = @("Silver","Silver","Silver","Silver","Gold","Gold","Gold","Platinum")
foreach ($cid in $customerIds) {
    $tier = $tiers | Get-Random
    $lifetime = switch ($tier) { "Silver" {Get-Random -Min 100 -Max 999} "Gold" {Get-Random -Min 1000 -Max 4999} "Platinum" {Get-Random -Min 5000 -Max 12000} }
    $balance = [math]::Floor($lifetime * 0.6)
    Run-Sql "EPCL_Loyalty" "IF NOT EXISTS (SELECT 1 FROM LoyaltyAccounts WHERE CustomerId='$cid') INSERT INTO LoyaltyAccounts (Id,CustomerId,PointsBalance,LifetimePoints,Tier,LastActivityAt,CreatedAt) VALUES (NEWID(),'$cid',$balance,$lifetime,'$tier',DATEADD(day,-$(Get-Random -Max 30),SYSDATETIMEOFFSET()),DATEADD(day,-60,SYSDATETIMEOFFSET()))"
}
# Referral codes
foreach ($cid in $customerIds) {
    $code = -join ((65..90)+(48..57) | Get-Random -Count 8 | ForEach-Object {[char]$_})
    Run-Sql "EPCL_Loyalty" "IF NOT EXISTS (SELECT 1 FROM ReferralCodes WHERE CustomerId='$cid') INSERT INTO ReferralCodes (Id,CustomerId,Code,TotalReferrals,TotalPointsEarned,CreatedAt) VALUES (NEWID(),'$cid','$code',$(Get-Random -Max 5),$(Get-Random -Max 2500),DATEADD(day,-50,SYSDATETIMEOFFSET()))"
}
Write-Host "  Loyalty accounts + referral codes created" -ForegroundColor Green

# ── 13. Seed Reports (daily summaries) ──
Write-Host "Seeding daily sales summaries..." -ForegroundColor Yellow
$stationIds = $stations | ForEach-Object { $_.Id.ToString() }
$fuelIds = @($petrolId, $dieselId, $cngId)
for ($d=0; $d -lt 30; $d++) {
    foreach ($sid in ($stationIds | Select-Object -First 5)) {
        foreach ($fid in $fuelIds) {
            $txnCount = Get-Random -Min 10 -Max 80
            $litres = [math]::Round($txnCount * (Get-Random -Min 15 -Max 35), 2)
            $revenue = [math]::Round($litres * (Get-Random -Min 76 -Max 115), 2)
            Run-Sql "EPCL_Reports" "INSERT INTO DailySalesSummaries (Id,StationId,FuelTypeId,Date,TotalTransactions,TotalLitresSold,TotalRevenue,LastUpdatedAt) VALUES (NEWID(),'$sid','$fid',DATEADD(day,-$d,CAST(GETDATE() AS DATE)),$txnCount,$litres,$revenue,SYSDATETIMEOFFSET())"
        }
    }
}
Write-Host "  Daily summaries created" -ForegroundColor Green

# ── Final counts ──
Write-Host "`n=== Final Record Counts ===" -ForegroundColor Cyan
$counts = @(
    @{DB="EPCL_Identity"; Q="SELECT COUNT(*) AS C FROM Users"},
    @{DB="EPCL_Stations"; Q="SELECT COUNT(*) AS C FROM Stations"},
    @{DB="EPCL_Inventory"; Q="SELECT (SELECT COUNT(*) FROM Tanks) + (SELECT COUNT(*) FROM StockLoadings) + (SELECT COUNT(*) FROM DipReadings) AS C"},
    @{DB="EPCL_Sales"; Q="SELECT (SELECT COUNT(*) FROM Transactions) + (SELECT COUNT(*) FROM Pumps) + (SELECT COUNT(*) FROM CustomerWallets) AS C"},
    @{DB="EPCL_Fraud"; Q="SELECT COUNT(*) AS C FROM FraudAlerts"},
    @{DB="EPCL_Audit"; Q="SELECT COUNT(*) AS C FROM AuditLogs"},
    @{DB="EPCL_Reports"; Q="SELECT COUNT(*) AS C FROM DailySalesSummaries"},
    @{DB="EPCL_Loyalty"; Q="SELECT (SELECT COUNT(*) FROM LoyaltyAccounts) + (SELECT COUNT(*) FROM ReferralCodes) AS C"}
)
foreach ($c in $counts) {
    $r = Run-Sql $c.DB $c.Q
    Write-Host "  $($c.DB): $($r.C) records" -ForegroundColor Green
}
Write-Host "`nSeeding complete!" -ForegroundColor Green
