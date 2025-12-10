#Requires -Version 5.1

<#
.SYNOPSIS
    AuctionService Windows 建置腳本

.DESCRIPTION
    此腳本用於在 Windows 環境中建置 AuctionService 專案，
    包含還原套件、編譯、測試和封裝等完整建置流程。

.PARAMETER Configuration
    建置配置 (Debug/Release)，預設為 Release

.PARAMETER SkipTests
    略過單元測試執行

.PARAMETER SkipIntegrationTests
    略過整合測試執行

.PARAMETER Clean
    在建置前清除所有輸出目錄

.PARAMETER Package
    產生 NuGet 套件

.PARAMETER Verbose
    啟用詳細輸出

.EXAMPLE
    .\build.ps1 -Configuration Debug -Verbose

.EXAMPLE
    .\build.ps1 -SkipTests -Package

.NOTES
    作者: AuctionService Team
    版本: 1.0.0
    需要: PowerShell 5.1+, .NET 10.0 SDK
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [Parameter(Mandatory = $false)]
    [switch]$SkipTests,

    [Parameter(Mandatory = $false)]
    [switch]$SkipIntegrationTests,

    [Parameter(Mandatory = $false)]
    [switch]$Clean,

    [Parameter(Mandatory = $false)]
    [switch]$Package,

    [Parameter(Mandatory = $false)]
    [switch]$Verbose
)

# 設定錯誤動作偏好
$ErrorActionPreference = "Stop"
$PSDefaultParameterValues['*:ErrorAction'] = 'Stop'

# 啟用詳細輸出
if ($Verbose) {
    $VerbosePreference = "Continue"
}

# 腳本變數
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
$SolutionFile = Join-Path $ProjectRoot "AuctionService.sln"
$OutputDir = Join-Path $ProjectRoot "artifacts"
$TestResultsDir = Join-Path $OutputDir "test-results"
$PackagesDir = Join-Path $OutputDir "packages"

# 專案路徑
$ApiProject = Join-Path $ProjectRoot "src\AuctionService.Api\AuctionService.Api.csproj"
$CoreProject = Join-Path $ProjectRoot "src\AuctionService.Core\AuctionService.Core.csproj"
$InfrastructureProject = Join-Path $ProjectRoot "src\AuctionService.Infrastructure\AuctionService.Infrastructure.csproj"
$SharedProject = Join-Path $ProjectRoot "src\AuctionService.Shared\AuctionService.Shared.csproj"

# 測試專案路徑
$UnitTestsProject = Join-Path $ProjectRoot "tests\AuctionService.UnitTests\AuctionService.UnitTests.csproj"
$IntegrationTestsProject = Join-Path $ProjectRoot "tests\AuctionService.IntegrationTests\AuctionService.IntegrationTests.csproj"
$ContractTestsProject = Join-Path $ProjectRoot "tests\AuctionService.ContractTests\AuctionService.ContractTests.csproj"

# 函數定義
function Write-Step {
    param([string]$Message)
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "✓ $Message" -ForegroundColor Green
}

function Write-Error {
    param([string]$Message)
    Write-Host "✗ $Message" -ForegroundColor Red
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠ $Message" -ForegroundColor Yellow
}

function Test-DotNetSdk {
    Write-Step "檢查 .NET SDK 版本"

    try {
        $dotnetVersion = & dotnet --version
        Write-Success ".NET SDK 版本: $dotnetVersion"

        # 檢查是否為 .NET 10.0
        if ($dotnetVersion -notlike "10.0.*") {
            Write-Warning "建議使用 .NET 10.0 SDK，目前版本: $dotnetVersion"
        }
    }
    catch {
        Write-Error ".NET SDK 未安裝或無法存取"
        throw "需要安裝 .NET 10.0 SDK"
    }
}

function Invoke-Clean {
    Write-Step "清除建置輸出"

    # 清除專案輸出目錄
    $cleanDirs = @(
        (Join-Path $ProjectRoot "src\AuctionService.Api\bin"),
        (Join-Path $ProjectRoot "src\AuctionService.Api\obj"),
        (Join-Path $ProjectRoot "src\AuctionService.Core\bin"),
        (Join-Path $ProjectRoot "src\AuctionService.Core\obj"),
        (Join-Path $ProjectRoot "src\AuctionService.Infrastructure\bin"),
        (Join-Path $ProjectRoot "src\AuctionService.Infrastructure\obj"),
        (Join-Path $ProjectRoot "src\AuctionService.Shared\bin"),
        (Join-Path $ProjectRoot "src\AuctionService.Shared\obj"),
        (Join-Path $ProjectRoot "tests\AuctionService.UnitTests\bin"),
        (Join-Path $ProjectRoot "tests\AuctionService.UnitTests\obj"),
        (Join-Path $ProjectRoot "tests\AuctionService.IntegrationTests\bin"),
        (Join-Path $ProjectRoot "tests\AuctionService.IntegrationTests\obj"),
        (Join-Path $ProjectRoot "tests\AuctionService.ContractTests\bin"),
        (Join-Path $ProjectRoot "tests\AuctionService.ContractTests\obj"),
        $OutputDir
    )

    foreach ($dir in $cleanDirs) {
        if (Test-Path $dir) {
            Write-Verbose "清除目錄: $dir"
            Remove-Item $dir -Recurse -Force
        }
    }

    Write-Success "建置輸出已清除"
}

function Invoke-Restore {
    Write-Step "還原 NuGet 套件"

    try {
        & dotnet restore $SolutionFile
        Write-Success "NuGet 套件還原完成"
    }
    catch {
        Write-Error "NuGet 套件還原失敗: $($_.Exception.Message)"
        throw
    }
}

function Invoke-Build {
    Write-Step "編譯專案 (配置: $Configuration)"

    try {
        $buildArgs = @("build", $SolutionFile, "--configuration", $Configuration)

        if ($Verbose) {
            $buildArgs += "--verbosity", "detailed"
        }

        & dotnet $buildArgs
        Write-Success "專案編譯完成"
    }
    catch {
        Write-Error "專案編譯失敗: $($_.Exception.Message)"
        throw
    }
}

function Invoke-UnitTests {
    Write-Step "執行單元測試"

    if (!(Test-Path $TestResultsDir)) {
        New-Item -ItemType Directory -Path $TestResultsDir -Force | Out-Null
    }

    try {
        $testArgs = @("test", $UnitTestsProject, "--configuration", $Configuration,
                     "--logger", "trx;LogFileName=unit-tests.trx",
                     "--results-directory", $TestResultsDir)

        if ($Verbose) {
            $testArgs += "--verbosity", "detailed"
        }

        & dotnet $testArgs
        Write-Success "單元測試執行完成"
    }
    catch {
        Write-Error "單元測試執行失敗: $($_.Exception.Message)"
        throw
    }
}

function Invoke-IntegrationTests {
    Write-Step "執行整合測試"

    if (!(Test-Path $TestResultsDir)) {
        New-Item -ItemType Directory -Path $TestResultsDir -Force | Out-Null
    }

    try {
        $testArgs = @("test", $IntegrationTestsProject, "--configuration", $Configuration,
                     "--logger", "trx;LogFileName=integration-tests.trx",
                     "--results-directory", $TestResultsDir)

        if ($Verbose) {
            $testArgs += "--verbosity", "detailed"
        }

        & dotnet $testArgs
        Write-Success "整合測試執行完成"
    }
    catch {
        Write-Error "整合測試執行失敗: $($_.Exception.Message)"
        throw
    }
}

function Invoke-ContractTests {
    Write-Step "執行契約測試"

    if (!(Test-Path $TestResultsDir)) {
        New-Item -ItemType Directory -Path $TestResultsDir -Force | Out-Null
    }

    try {
        $testArgs = @("test", $ContractTestsProject, "--configuration", $Configuration,
                     "--logger", "trx;LogFileName=contract-tests.trx",
                     "--results-directory", $TestResultsDir)

        if ($Verbose) {
            $testArgs += "--verbosity", "detailed"
        }

        & dotnet $testArgs
        Write-Success "契約測試執行完成"
    }
    catch {
        Write-Error "契約測試執行失敗: $($_.Exception.Message)"
        throw
    }
}

function Invoke-Packaging {
    Write-Step "產生 NuGet 套件"

    if (!(Test-Path $PackagesDir)) {
        New-Item -ItemType Directory -Path $PackagesDir -Force | Out-Null
    }

    $projectsToPack = @($CoreProject, $InfrastructureProject, $SharedProject)

    foreach ($project in $projectsToPack) {
        try {
            $packArgs = @("pack", $project, "--configuration", $Configuration,
                         "--output", $PackagesDir, "--no-build")

            if ($Verbose) {
                $packArgs += "--verbosity", "detailed"
            }

            & dotnet $packArgs
        }
        catch {
            Write-Error "NuGet 套件產生失敗 for $($project): $($_.Exception.Message)"
            throw
        }
    }

    Write-Success "NuGet 套件產生完成"
}

function Show-Summary {
    Write-Step "建置摘要"

    Write-Host "配置: $Configuration" -ForegroundColor White
    Write-Host "輸出目錄: $OutputDir" -ForegroundColor White

    if (Test-Path $TestResultsDir) {
        $trxFiles = Get-ChildItem $TestResultsDir -Filter "*.trx" -ErrorAction SilentlyContinue
        Write-Host "測試結果: $($trxFiles.Count) 個測試報告檔案" -ForegroundColor White
    }

    if (Test-Path $PackagesDir) {
        $nupkgFiles = Get-ChildItem $PackagesDir -Filter "*.nupkg" -ErrorAction SilentlyContinue
        Write-Host "NuGet 套件: $($nupkgFiles.Count) 個套件檔案" -ForegroundColor White
    }

    Write-Success "建置程序完成"
}

# 主程式
try {
    Write-Step "開始 AuctionService 建置程序"
    Write-Host "專案根目錄: $ProjectRoot" -ForegroundColor White
    Write-Host "解決方案檔案: $SolutionFile" -ForegroundColor White

    # 環境檢查
    Test-DotNetSdk

    # 建置步驟
    if ($Clean) {
        Invoke-Clean
    }

    Invoke-Restore
    Invoke-Build

    if (!$SkipTests) {
        Invoke-UnitTests

        if (!$SkipIntegrationTests) {
            Invoke-IntegrationTests
        }

        Invoke-ContractTests
    }

    if ($Package) {
        Invoke-Packaging
    }

    # 顯示摘要
    Show-Summary

    Write-Success "所有建置步驟完成！"
    exit 0
}
catch {
    Write-Error "建置程序失敗: $($_.Exception.Message)"

    if ($Verbose) {
        Write-Error "詳細錯誤資訊:"
        Write-Error $_.Exception.StackTrace
    }

    exit 1
}