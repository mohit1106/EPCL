# ============================================================
# EPCL Platform - Start All Backend Services
# Run: .\scripts\start-all.ps1
# ============================================================

$ErrorActionPreference = "Continue"

Write-Host ""
Write-Host "  EPCL - Starting All Backend Services" -ForegroundColor Cyan
Write-Host ""

$root = Split-Path $PSScriptRoot -Parent

$services = @(
    @{ Name = "Gateway";            Path = "$root\src\Gateway\EPCLGateway";                                               Port = 5000 },
    @{ Name = "IdentityService";    Path = "$root\src\Services\IdentityService\src\IdentityService.Api";                   Port = 5217 },
    @{ Name = "StationService";     Path = "$root\src\Services\StationService\src\StationService.Api";                     Port = 5143 },
    @{ Name = "InventoryService";   Path = "$root\src\Services\InventoryService\src\InventoryService.Api";                 Port = 5134 },
    @{ Name = "SalesService";       Path = "$root\src\Services\SalesService\src\SalesService.Api";                         Port = 5167 },
    @{ Name = "FraudDetection";     Path = "$root\src\Services\FraudDetectionService\src\FraudDetectionService.Api";       Port = 5237 },
    @{ Name = "NotificationService";Path = "$root\src\Services\NotificationService\src\NotificationService.Api";           Port = 5037 },
    @{ Name = "ReportingService";   Path = "$root\src\Services\ReportingService\src\ReportingService.Api";                 Port = 5062 },
    @{ Name = "AuditService";       Path = "$root\src\Services\AuditService\src\AuditService.Api";                         Port = 5268 },
    @{ Name = "LoyaltyService";     Path = "$root\src\Services\LoyaltyService\src\LoyaltyService.Api";                     Port = 5192 },
    @{ Name = "AIAnalyticsService"; Path = "$root\src\Services\AIAnalyticsService\src\AIAnalyticsService.Api";             Port = 5010 }
)

$jobs = @()

foreach ($svc in $services) {
    Write-Host "  > Starting $($svc.Name) on port $($svc.Port)..." -ForegroundColor Yellow
    $job = Start-Job -ScriptBlock {
        param($path)
        Set-Location $path
        dotnet run --no-build 2>&1
    } -ArgumentList $svc.Path
    $jobs += @{ Job = $job; Service = $svc }
}

Write-Host ""
Write-Host "  Waiting 15 seconds for services to start..." -ForegroundColor Gray
Write-Host ""
Start-Sleep -Seconds 15

# Health check
$healthy = 0
$total = $services.Count
foreach ($svc in $services) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:$($svc.Port)/health" -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
        if ($response.StatusCode -eq 200) {
            Write-Host "  OK $($svc.Name) - Healthy (port $($svc.Port))" -ForegroundColor Green
            $healthy++
        }
        else {
            Write-Host "  FAIL $($svc.Name) - Status $($response.StatusCode)" -ForegroundColor Red
        }
    }
    catch {
        Write-Host "  FAIL $($svc.Name) - Not responding on port $($svc.Port)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "  $healthy/$total services healthy" -ForegroundColor $(if ($healthy -eq $total) { "Green" } else { "Yellow" })
Write-Host ""
Write-Host "  Press Ctrl+C to stop all services..." -ForegroundColor Gray
Write-Host ""

# Keep running until Ctrl+C
try {
    while ($true) { Start-Sleep -Seconds 5 }
}
finally {
    Write-Host "  Stopping all services..." -ForegroundColor Yellow
    foreach ($j in $jobs) {
        Stop-Job -Job $j.Job -ErrorAction SilentlyContinue
        Remove-Job -Job $j.Job -Force -ErrorAction SilentlyContinue
    }
    Write-Host "  All services stopped." -ForegroundColor Green
}
