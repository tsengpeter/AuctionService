#Requires -Version 5.1

param(
    [Parameter(Mandatory = $false)]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [Parameter(Mandatory = $false)]
    [switch]$SkipTests
)

Write-Host "Configuration: $Configuration"
Write-Host "SkipTests: $SkipTests"

try {
    Write-Host "Starting build process..."
    
    # Get script and project paths
    $ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $ProjectRoot = Split-Path -Parent $ScriptDir
    $SolutionFile = Join-Path $ProjectRoot "AuctionService.sln"
    $UnitTestsProject = Join-Path $ProjectRoot "tests\AuctionService.UnitTests\AuctionService.UnitTests.csproj"
    
    Write-Host "Project root: $ProjectRoot"
    Write-Host "Solution file: $SolutionFile"
    
    # Test dotnet version
    $version = & dotnet --version
    Write-Host "DotNet version: $version"
    
    # Test build
    & dotnet build $SolutionFile --configuration $Configuration
    
    if (!$SkipTests) {
        Write-Host "Running tests..."
        & dotnet test $UnitTestsProject --configuration $Configuration
    }
    
    Write-Host "Build completed successfully!"
    exit 0
}
catch {
    Write-Host "Build failed: $($_.Exception.Message)"
    exit 1
}
