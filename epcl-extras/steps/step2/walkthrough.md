# Step 2 — Identity Service Walkthrough

## Summary

The Identity Service is now fully implemented across all 4 Clean Architecture layers with **32 completed tasks** and a clean build. This service handles authentication, authorization, and user management for the entire EPCL platform.

---

## Architecture

```
IdentityService.API (ASP.NET 10 Web API)
  └── IdentityService.Infrastructure (EF Core, MailKit, RabbitMQ, JWT)
       └── IdentityService.Application (MediatR CQRS, FluentValidation, AutoMapper)
            └── IdentityService.Domain (Entities, Enums, Exceptions, Interfaces)
```

---

## Domain Layer

### Entities (5)
| Entity | Description |
|--------|-------------|
| [User](file:///d:/projects/epcl-fuel-management-system/src/Services/IdentityService/src/IdentityService.Domain/Entities/User.cs) | Core user with lockout logic, Google OAuth fields |
| [UserProfile](file:///d:/projects/epcl-fuel-management-system/src/Services/IdentityService/src/IdentityService.Domain/Entities/UserProfile.cs) | One-to-one profile with address, language, station |
| [RefreshToken](file:///d:/projects/epcl-fuel-management-system/src/Services/IdentityService/src/IdentityService.Domain/Entities/RefreshToken.cs) | JWT refresh tokens with rotation support |
| [OtpRequest](file:///d:/projects/epcl-fuel-management-system/src/Services/IdentityService/src/IdentityService.Domain/Entities/OtpRequest.cs) | 6-digit OTP with expiry and purpose tracking |
| [ProcessedEvent](file:///d:/projects/epcl-fuel-management-system/src/Services/IdentityService/src/IdentityService.Domain/Entities/ProcessedEvent.cs) | RabbitMQ consumer idempotency |

### Enums: `UserRole` (5 roles), `AuthProvider` (Local/Google), `OtpPurpose` (4 types)
### Exceptions: `DomainException`, `NotFoundException`, `InvalidCredentialsException`, `AccountLockedException`, `DuplicateEntityException`

---

## Application Layer

### CQRS Commands (11)
| Command | Key Behavior |
|---------|-------------|
| RegisterUser | BCrypt hash (workFactor 12), duplicate check, publish `UserRegisteredEvent` |
| LoginUser | Lockout after 5 failures (15 min), publish `UserAccountLockedEvent` |
| GoogleLogin | Validate Google ID token, auto-register as Customer, link existing accounts |
| RefreshToken | Token rotation — revoke old, issue new pair |
| LogoutUser | Revoke all refresh tokens for user |
| ForgotPassword | 6-digit OTP, 10-min expiry, styled HTML email, anti-enumeration response |
| ResetPassword | OTP validation, BCrypt rehash, lockout reset |
| ChangePassword | Verify current password before update |
| VerifyOtp | Generic OTP verification for any purpose |
| UpdateUserRole | Admin-only role change with audit event |
| LockUser | Admin lock/unlock with indefinite lockout support |

### CQRS Queries (2)
- `GetCurrentUser` — by JWT claim
- `GetAllUsers` — paginated with role/active/search filters

### Validators: RegisterUser, LoginUser, ResetPassword (password policy: 8+ chars, upper, lower, digit, special)

---

## Infrastructure Layer

### Database
- **IdentityDbContext** with 5 entity configurations
- **InitialCreate migration** generated
- Tables: `Users`, `UserProfiles`, `RefreshTokens`, `OtpRequests`, `ProcessedEvents`

### Services
| Service | Technology |
|---------|-----------|
| [JwtService](file:///d:/projects/epcl-fuel-management-system/src/Services/IdentityService/src/IdentityService.Infrastructure/Services/JwtService.cs) | `System.IdentityModel.Tokens.Jwt` — 15-min access, 7-day refresh |
| [GmailSmtpEmailService](file:///d:/projects/epcl-fuel-management-system/src/Services/IdentityService/src/IdentityService.Infrastructure/Email/GmailSmtpEmailService.cs) | MailKit with STARTTLS, multipart HTML |
| [EmailTemplateService](file:///d:/projects/epcl-fuel-management-system/src/Services/IdentityService/src/IdentityService.Infrastructure/Email/EmailTemplateService.cs) | Embedded HTML resources with `{{Variable}}` substitution |
| [GoogleAuthService](file:///d:/projects/epcl-fuel-management-system/src/Services/IdentityService/src/IdentityService.Infrastructure/Services/GoogleAuthService.cs) | `Google.Apis.Auth` ID token validation |
| [RabbitMqPublisher](file:///d:/projects/epcl-fuel-management-system/src/Services/IdentityService/src/IdentityService.Infrastructure/Messaging/RabbitMqPublisher.cs) | RabbitMQ.Client 7.x async API, `epcl.events` topic exchange |

### Email Templates (embedded resources)
- `otp.html` — EPCL-branded OTP verification
- `welcome.html` — Role-based welcome with feature highlights

---

## API Layer

### Controllers
| Controller | Endpoints | Auth |
|-----------|-----------|------|
| [AuthController](file:///d:/projects/epcl-fuel-management-system/src/Services/IdentityService/src/IdentityService.API/Controllers/AuthController.cs) | 9 (register, login, google-login, refresh, logout, forgot/reset/change-password, verify-otp) | Mixed |
| [UsersController](file:///d:/projects/epcl-fuel-management-system/src/Services/IdentityService/src/IdentityService.API/Controllers/UsersController.cs) | 6 (me, list, get, role, lock, delete) | JWT + Admin roles |

### Middleware
- **CorrelationIdMiddleware** — Injects `X-Correlation-ID`, pushes to Serilog context
- **GlobalExceptionMiddleware** — Maps exceptions to HTTP codes (400/401/404/409/422/423/500)

### Program.cs Configuration
- Serilog → Console + File + Elasticsearch (`epcl-identity-*` index)
- JWT Bearer auth with configurable expiry
- Swagger UI with Bearer token support
- CORS for `localhost:4200`
- Health check on SQL Server
- `.env` file loading in Development

---

## Build Verification

```
✅ IdentityService.Domain      → Build succeeded
✅ IdentityService.Application → Build succeeded
✅ IdentityService.Infrastructure → Build succeeded
✅ IdentityService.API          → Build succeeded
✅ Full solution build           → Build succeeded
✅ EF Core migration generated  → InitialCreate
```

## NuGet Packages Used

| Layer | Packages |
|-------|----------|
| Application | MediatR 12.4.1, FluentValidation 11.11.0, AutoMapper 13.0.1, BCrypt.Net-Next 4.0.3 |
| Infrastructure | EF Core SqlServer 9.0.4, MailKit 4.10.0, RabbitMQ.Client 7.1.2, Google.Apis.Auth 1.69.0, StackExchange.Redis 2.8.24, JWT 8.7.0 |
| API | Serilog.AspNetCore 9.0.0, Swashbuckle 7.3.1, FluentValidation.AspNetCore 11.3.0, HealthChecks.SqlServer 9.0.0 |
