FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/IdentityService.API/IdentityService.API.csproj", "src/IdentityService.API/"]
COPY ["src/IdentityService.Infrastructure/IdentityService.Infrastructure.csproj", "src/IdentityService.Infrastructure/"]
COPY ["src/IdentityService.Application/IdentityService.Application.csproj", "src/IdentityService.Application/"]
COPY ["src/IdentityService.Domain/IdentityService.Domain.csproj", "src/IdentityService.Domain/"]

RUN dotnet restore "src/IdentityService.API/IdentityService.API.csproj"

COPY . .
WORKDIR "/src/src/IdentityService.API"
RUN dotnet publish "IdentityService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "IdentityService.API.dll"]
