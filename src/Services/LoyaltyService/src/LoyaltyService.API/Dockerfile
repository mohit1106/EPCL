FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/LoyaltyService.API/LoyaltyService.API.csproj", "src/LoyaltyService.API/"]
COPY ["src/LoyaltyService.Infrastructure/LoyaltyService.Infrastructure.csproj", "src/LoyaltyService.Infrastructure/"]
COPY ["src/LoyaltyService.Application/LoyaltyService.Application.csproj", "src/LoyaltyService.Application/"]
COPY ["src/LoyaltyService.Domain/LoyaltyService.Domain.csproj", "src/LoyaltyService.Domain/"]
RUN dotnet restore "src/LoyaltyService.API/LoyaltyService.API.csproj"
COPY . .
WORKDIR "/src/src/LoyaltyService.API"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LoyaltyService.API.dll"]
