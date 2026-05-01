# Starting All EPCL Services

This guide provides the sequential commands to start all components of the EPCL Fuel Management System manually.

## 🚀 The Quick Way (Automated Script)

All services can be started at once using the provided PowerShell script. Open a PowerShell window in the project root and run:

```powershell
.\start_all.ps1
```

This script will automatically start Docker, all 12 .NET Microservices, the Gateway, the Angular Frontend, and the Static Landing page in the background.

---

## 🛠️ The Manual Way (Sequential Commands)

If you need to start the services individually for debugging or development, follow this sequence:

### 1. Database & Infrastructure
*Ensure SQL Server is running locally on your machine.*

Start the Docker containers (Redis, ElasticSearch, Kibana):
```powershell
docker-compose up -d
```

### 2. Backend Microservices
Open separate terminals for each service or run them in the background. Navigate to each directory and run:

```powershell
# 1. API Gateway
cd src/Gateway/EPCLGateway
dotnet run

# 2. Identity Service
cd src/Services/IdentityService/src/IdentityService.API
dotnet run

# 3. Station Service
cd src/Services/StationService/src/StationService.API
dotnet run

# 4. Inventory Service
cd src/Services/InventoryService/src/InventoryService.API
dotnet run

# 5. Sales Service
cd src/Services/SalesService/src/SalesService.API
dotnet run

# 6. Fraud Detection Service
cd src/Services/FraudDetectionService/src/FraudDetectionService.API
dotnet run

# 7. Notification Service
cd src/Services/NotificationService/src/NotificationService.API
dotnet run

# 8. Reporting Service
cd src/Services/ReportingService/src/ReportingService.API
dotnet run

# 9. Audit Service
cd src/Services/AuditService/src/AuditService.API
dotnet run

# 10. Loyalty Service
cd src/Services/LoyaltyService/src/LoyaltyService.API
dotnet run

# 11. AI Analytics Service
cd src/Services/AIAnalyticsService/src/AIAnalyticsService.API
dotnet run

# 12. Document Service
cd src/Services/DocumentService/src/DocumentService.API
dotnet run
```

### 3. Frontend Application (Angular)
In a new terminal window:
```powershell
cd src/Services/Frontend/epcl-angular
npm start
# OR
npx ng serve
```
*Access at: http://localhost:4200*

### 4. Landing Page
In a new terminal window:
```powershell
cd landing
npx serve -l 5500
```
*Access at: http://localhost:5500*

---

## 🛑 Stopping Services

If you used the automated script (`.\start_all.ps1`) and need to stop the background processes:

```powershell
# Stop all .NET microservices
Get-Process dotnet | Stop-Process -Force

# Stop the node processes (Angular and Landing Page)
Get-Process node | Stop-Process -Force

# Stop Docker containers
docker-compose down
```
