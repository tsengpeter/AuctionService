# AuctionService 壓力測試啟動腳本
# 此腳本會啟動 AuctionService API 和壓力測試

Write-Host "╔═══════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   AuctionService 壓力測試啟動器               ║" -ForegroundColor Cyan
Write-Host "╚═══════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# 檢查是否已經有 API 在運行
Write-Host "🔍 檢查 AuctionService API 狀態..." -ForegroundColor Yellow

try {
    $response = Invoke-WebRequest -Uri "http://localhost:5106/health" -Method GET -TimeoutSec 2 -ErrorAction SilentlyContinue
    if ($response.StatusCode -eq 200) {
        Write-Host "✓ AuctionService API 已在運行中" -ForegroundColor Green
        Write-Host ""
        
        $runTest = Read-Host "是否立即執行壓力測試? (Y/N)"
        if ($runTest -eq "Y" -or $runTest -eq "y") {
            Write-Host ""
            Write-Host "🚀 啟動壓力測試..." -ForegroundColor Cyan
            dotnet run
        }
        exit
    }
} catch {
    Write-Host "⚠ AuctionService API 未運行" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "📋 壓力測試需要 AuctionService API 運行中" -ForegroundColor Yellow
Write-Host ""
Write-Host "請選擇操作:" -ForegroundColor Cyan
Write-Host "  1) 在新視窗啟動 API 並執行壓測" -ForegroundColor White
Write-Host "  2) 僅執行壓測（手動啟動 API）" -ForegroundColor White
Write-Host "  3) 取消" -ForegroundColor White
Write-Host ""

$choice = Read-Host "請輸入選項 (1-3)"

switch ($choice) {
    "1" {
        Write-Host ""
        Write-Host "🚀 啟動 AuctionService API..." -ForegroundColor Cyan
        
        # 啟動 API 在新視窗
        $apiPath = "..\..\src\AuctionService.Api"
        
        if (Test-Path $apiPath) {
            Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$apiPath'; Write-Host '啟動 AuctionService API...' -ForegroundColor Green; dotnet run"
            
            Write-Host "⏳ 等待 API 啟動..." -ForegroundColor Yellow
            Start-Sleep -Seconds 5
            
            # 等待 API 就緒
            $maxRetries = 12
            $retryCount = 0
            $apiReady = $false
            
            while ($retryCount -lt $maxRetries -and -not $apiReady) {
                try {
                    $response = Invoke-WebRequest -Uri "http://localhost:5106/health" -Method GET -TimeoutSec 2 -ErrorAction SilentlyContinue
                    if ($response.StatusCode -eq 200) {
                        $apiReady = $true
                        Write-Host "✓ API 已就緒" -ForegroundColor Green
                    }
                } catch {
                    $retryCount++
                    Write-Host "." -NoNewline
                    Start-Sleep -Seconds 2
                }
            }
            
            if ($apiReady) {
                Write-Host ""
                Write-Host "🚀 啟動壓力測試..." -ForegroundColor Cyan
                Write-Host ""
                dotnet run
            } else {
                Write-Host ""
                Write-Host "✗ API 啟動失敗或超時" -ForegroundColor Red
                Write-Host "請手動檢查 API 狀態" -ForegroundColor Yellow
            }
        } else {
            Write-Host "✗ 找不到 API 專案路徑: $apiPath" -ForegroundColor Red
        }
    }
    "2" {
        Write-Host ""
        Write-Host "請手動啟動 AuctionService API，然後按任意鍵繼續..." -ForegroundColor Yellow
        Write-Host ""
        Write-Host "啟動命令:" -ForegroundColor Cyan
        Write-Host "  cd ..\..\src\AuctionService.Api" -ForegroundColor White
        Write-Host "  dotnet run" -ForegroundColor White
        Write-Host ""
        
        $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
        
        Write-Host ""
        Write-Host "🚀 啟動壓力測試..." -ForegroundColor Cyan
        Write-Host ""
        dotnet run
    }
    "3" {
        Write-Host ""
        Write-Host "已取消" -ForegroundColor Yellow
    }
    default {
        Write-Host ""
        Write-Host "✗ 無效的選項" -ForegroundColor Red
    }
}
