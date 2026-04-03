FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/SalesService.API/SalesService.API.csproj", "src/SalesService.API/"]
COPY ["src/SalesService.Infrastructure/SalesService.Infrastructure.csproj", "src/SalesService.Infrastructure/"]
COPY ["src/SalesService.Application/SalesService.Application.csproj", "src/SalesService.Application/"]
COPY ["src/SalesService.Domain/SalesService.Domain.csproj", "src/SalesService.Domain/"]
RUN dotnet restore "src/SalesService.API/SalesService.API.csproj"
COPY . .
WORKDIR "/src/src/SalesService.API"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SalesService.API.dll"]
