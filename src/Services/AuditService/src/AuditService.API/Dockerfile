FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/AuditService.API/AuditService.API.csproj", "src/AuditService.API/"]
COPY ["src/AuditService.Infrastructure/AuditService.Infrastructure.csproj", "src/AuditService.Infrastructure/"]
COPY ["src/AuditService.Application/AuditService.Application.csproj", "src/AuditService.Application/"]
COPY ["src/AuditService.Domain/AuditService.Domain.csproj", "src/AuditService.Domain/"]
RUN dotnet restore "src/AuditService.API/AuditService.API.csproj"
COPY . .
WORKDIR "/src/src/AuditService.API"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AuditService.API.dll"]
