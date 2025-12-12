# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/AuctionService.Api/AuctionService.Api.csproj", "src/AuctionService.Api/"]
COPY ["src/AuctionService.Core/AuctionService.Core.csproj", "src/AuctionService.Core/"]
COPY ["src/AuctionService.Infrastructure/AuctionService.Infrastructure.csproj", "src/AuctionService.Infrastructure/"]
COPY ["src/AuctionService.Shared/AuctionService.Shared.csproj", "src/AuctionService.Shared/"]

RUN dotnet restore "src/AuctionService.Api/AuctionService.Api.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/src/AuctionService.Api"
RUN dotnet build "AuctionService.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "AuctionService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "AuctionService.Api.dll"]