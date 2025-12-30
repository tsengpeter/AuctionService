# Build script for MemberService

param(
    [string]$Configuration = "Release"
)

Write-Host "Building MemberService..." -ForegroundColor Green

# Restore packages
dotnet restore

# Build all projects
dotnet build --configuration $Configuration

# Run tests
dotnet test --configuration $Configuration --no-build

# Publish API
dotnet publish src/MemberService.API/MemberService.API.csproj --configuration $Configuration --output publish

Write-Host "Build completed successfully!" -ForegroundColor Green