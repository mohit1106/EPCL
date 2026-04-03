FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/StationService.API/StationService.API.csproj", "src/StationService.API/"]
COPY ["src/StationService.Infrastructure/StationService.Infrastructure.csproj", "src/StationService.Infrastructure/"]
COPY ["src/StationService.Application/StationService.Application.csproj", "src/StationService.Application/"]
COPY ["src/StationService.Domain/StationService.Domain.csproj", "src/StationService.Domain/"]

RUN dotnet restore "src/StationService.API/StationService.API.csproj"
COPY . .
WORKDIR "/src/src/StationService.API"
RUN dotnet publish "StationService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "StationService.API.dll"]
