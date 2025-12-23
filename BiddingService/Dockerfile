# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/BiddingService.Api/BiddingService.Api.csproj", "BiddingService.Api/"]
COPY ["src/BiddingService.Core/BiddingService.Core.csproj", "BiddingService.Core/"]
COPY ["src/BiddingService.Infrastructure/BiddingService.Infrastructure.csproj", "BiddingService.Infrastructure/"]
COPY ["src/BiddingService.Shared/BiddingService.Shared.csproj", "BiddingService.Shared/"]

RUN dotnet restore "BiddingService.Api/BiddingService.Api.csproj"

# Copy everything else and build
COPY src/ .
WORKDIR "/src/BiddingService.Api"
RUN dotnet build "BiddingService.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "BiddingService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BiddingService.Api.dll"]