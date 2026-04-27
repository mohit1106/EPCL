$services = @(
    "src/Gateway/EPCLGateway",
    "src/Services/IdentityService/src/IdentityService.API",
    "src/Services/StationService/src/StationService.API",
    "src/Services/InventoryService/src/InventoryService.API",
    "src/Services/SalesService/src/SalesService.API",
    "src/Services/FraudDetectionService/src/FraudDetectionService.API",
    "src/Services/NotificationService/src/NotificationService.API",
    "src/Services/ReportingService/src/ReportingService.API",
    "src/Services/AuditService/src/AuditService.API",
    "src/Services/LoyaltyService/src/LoyaltyService.API",
    "src/Services/AIAnalyticsService/src/AIAnalyticsService.API",
    "src/Services/DocumentService/src/DocumentService.API"
)

# Load .env variables into process
Get-Content -Path "d:\projects\epcl-fuel-management-system\.env" | ForEach-Object {
    if ($_ -match "^\s*([^#\s][^=]+)=(.*)$") {
        [Environment]::SetEnvironmentVariable($matches[1].Trim(), $matches[2].Trim(), "Process")
    }
}
[Environment]::SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development", "Process")

foreach ($service in $services) {
    Write-Host "Starting $service..."
    $serviceName = $service.Split('/')[-1]
    
    # Run in background via Start-Process
    Start-Process -FilePath "dotnet" -ArgumentList "run" -WorkingDirectory "d:\projects\epcl-fuel-management-system\$service" -WindowStyle Hidden -RedirectStandardOutput "d:\projects\epcl-fuel-management-system\tmp\$serviceName.log" -RedirectStandardError "d:\projects\epcl-fuel-management-system\tmp\$serviceName.err.log"
}

Write-Host "Starting Angular Frontend..."
Start-Process -FilePath "npx.cmd" -ArgumentList "ng serve" -WorkingDirectory "d:\projects\epcl-fuel-management-system\src\Services\Frontend\epcl-angular" -WindowStyle Hidden -RedirectStandardOutput "d:\projects\epcl-fuel-management-system\tmp\angular.log" -RedirectStandardError "d:\projects\epcl-fuel-management-system\tmp\angular.err.log"

Write-Host "All services started."
