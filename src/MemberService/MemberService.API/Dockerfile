# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Copy solution and projects
COPY ["MemberService.sln", "."]
COPY ["src/MemberService/MemberService.Domain/MemberService.Domain.csproj", "src/MemberService/MemberService.Domain/"]
COPY ["src/MemberService/MemberService.Application/MemberService.Application.csproj", "src/MemberService/MemberService.Application/"]
COPY ["src/MemberService/MemberService.Infrastructure/MemberService.Infrastructure.csproj", "src/MemberService/MemberService.Infrastructure/"]
COPY ["src/MemberService/MemberService.API/MemberService.API.csproj", "src/MemberService/MemberService.API/"]

# Restore dependencies
RUN dotnet restore "MemberService.sln"

# Copy source code
COPY . .

# Build
RUN dotnet build "MemberService.sln" -c Release --no-restore

# Publish
RUN dotnet publish "src/MemberService/MemberService.API/MemberService.API.csproj" -c Release --no-build -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app

# Install curl for health checks
RUN apk add --no-cache curl

COPY --from=build /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000

ENTRYPOINT ["dotnet", "MemberService.API.dll"]
