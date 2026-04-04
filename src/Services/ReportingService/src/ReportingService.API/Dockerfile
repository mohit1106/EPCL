FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/ReportingService.API/ReportingService.API.csproj", "src/ReportingService.API/"]
COPY ["src/ReportingService.Infrastructure/ReportingService.Infrastructure.csproj", "src/ReportingService.Infrastructure/"]
COPY ["src/ReportingService.Application/ReportingService.Application.csproj", "src/ReportingService.Application/"]
COPY ["src/ReportingService.Domain/ReportingService.Domain.csproj", "src/ReportingService.Domain/"]
RUN dotnet restore "src/ReportingService.API/ReportingService.API.csproj"
COPY . .
WORKDIR "/src/src/ReportingService.API"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ReportingService.API.dll"]
