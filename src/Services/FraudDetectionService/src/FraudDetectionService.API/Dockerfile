FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/FraudDetectionService.API/FraudDetectionService.API.csproj", "src/FraudDetectionService.API/"]
COPY ["src/FraudDetectionService.Infrastructure/FraudDetectionService.Infrastructure.csproj", "src/FraudDetectionService.Infrastructure/"]
COPY ["src/FraudDetectionService.Application/FraudDetectionService.Application.csproj", "src/FraudDetectionService.Application/"]
COPY ["src/FraudDetectionService.Domain/FraudDetectionService.Domain.csproj", "src/FraudDetectionService.Domain/"]
RUN dotnet restore "src/FraudDetectionService.API/FraudDetectionService.API.csproj"
COPY . .
WORKDIR "/src/src/FraudDetectionService.API"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FraudDetectionService.API.dll"]
