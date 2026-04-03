# EPCL — Eleven Petroleum Corporation Limited

> Enterprise Fuel Management Platform

## Overview

EPCL is a production-grade, enterprise-level fuel management platform built with a microservices architecture. It provides real-time petroleum intelligence, station management, inventory tracking, fraud detection, and customer-facing fuel ordering with digital wallet and loyalty programs.

## Architecture

- **Backend:** .NET 10 (Clean Architecture + CQRS via MediatR)
- **Frontend:** Angular 17 (NgRx, TailwindCSS, SignalR)
- **Database:** SQL Server (database-per-service) via EF Core 9
- **Messaging:** RabbitMQ (Choreography Saga pattern)
- **Caching:** Redis
- **Logging:** Serilog → Elasticsearch + Kibana
- **Auth:** JWT + Google OAuth 2.0
- **Payments:** Razorpay
- **Email:** Gmail SMTP via MailKit
- **Gateway:** Ocelot API Gateway
- **Real-time:** SignalR

## Microservices

| # | Service | Port | Database |
|---|---------|------|----------|
| 1 | IdentityService | 5001 | EPCL_Identity |
| 2 | StationService | 5002 | EPCL_Stations |
| 3 | InventoryService | 5003 | EPCL_Inventory |
| 4 | SalesService | 5004 | EPCL_Sales |
| 5 | ReportingService | 5005 | EPCL_Reports |
| 6 | NotificationService | 5006 | EPCL_Notifications |
| 7 | FraudDetectionService | 5007 | EPCL_Fraud |
| 8 | AuditService | 5008 | EPCL_Audit |
| 9 | LoyaltyService | 5009 | EPCL_Loyalty |
| — | EPCLGateway | 5000 | — |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/)
- [Node.js 18+](https://nodejs.org/) (for Angular)
- [SQL Server Express](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (localhost\SQLEXPRESS)
- [RabbitMQ](https://www.rabbitmq.com/) (localhost:5672)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for Redis, Elasticsearch, Kibana)

## Getting Started

### 1. Start Infrastructure Services

```bash
# Start Redis, Elasticsearch, and Kibana
docker-compose up -d

# Verify services are running
# Redis:          localhost:6379
# Elasticsearch:  http://localhost:9200
# Kibana:         http://localhost:5601
```

### 2. Configure Environment

```bash
# Copy .env and fill in your secrets
cp .env .env.local
```

### 3. Run Migrations (per service)

```bash
cd src/Services/IdentityService
dotnet ef migrations add InitialCreate --project src/IdentityService.Infrastructure --startup-project src/IdentityService.API
dotnet ef database update --project src/IdentityService.Infrastructure --startup-project src/IdentityService.API
```

### 4. Run a Service

```bash
cd src/Services/IdentityService/src/IdentityService.API
dotnet run
```

### 5. Run the Angular Frontend

```bash
cd src/Services/Frontend/epcl-angular
npm install
ng serve
```

### 6. Access the Platform

| Component | URL |
|-----------|-----|
| API Gateway | http://localhost:5000 |
| Angular App | http://localhost:4200 |
| Swagger (per service) | http://localhost:{port}/swagger |
| RabbitMQ Management | http://localhost:15672 |
| Kibana | http://localhost:5601 |

## Project Structure

```
epcl-fuel-management-system/
├── docker-compose.yml          # Redis + Elasticsearch + Kibana
├── .env                        # Secrets (git-ignored)
├── landing/                    # Standalone Three.js landing page
│   └── index.html
└── src/
    ├── Gateway/
    │   └── EPCLGateway/
    └── Services/
        ├── IdentityService/
        ├── StationService/
        ├── InventoryService/
        ├── SalesService/
        ├── ReportingService/
        ├── NotificationService/
        ├── FraudDetectionService/
        ├── AuditService/
        ├── LoyaltyService/
        └── Frontend/
            └── epcl-angular/
```

## User Roles

- **Admin:** Full platform control, system monitoring, fraud intelligence
- **Dealer:** Station operations, fuel sales, inventory management
- **Customer:** Fuel ordering, wallet, loyalty rewards, station discovery

## License

© 2025 Eleven Petroleum Corporation Limited. All rights reserved.
