# AuctionService Development Startup Script
# 快速啟動開發環境

Write-Host "=== AuctionService 開發環境啟動 ===" -ForegroundColor Cyan
Write-Host ""

# 檢查 Docker 是否運行
Write-Host "檢查 Docker 狀態..." -ForegroundColor Yellow
$dockerRunning = docker info 2>$null
if (-not $dockerRunning) {
    Write-Host "❌ Docker 未運行，請先啟動 Docker Desktop" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Docker 正在運行" -ForegroundColor Green
Write-Host ""

# 停止現有容器
Write-Host "停止現有容器..." -ForegroundColor Yellow
docker-compose down 2>$null
Write-Host ""

# 啟動服務
Write-Host "啟動 PostgreSQL 和 AuctionService..." -ForegroundColor Yellow
docker-compose up -d

# 等待服務健康檢查
Write-Host ""
Write-Host "等待服務啟動..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

# 檢查服務狀態
$maxRetries = 10
$retryCount = 0
$serviceReady = $false

while ($retryCount -lt $maxRetries -and -not $serviceReady) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5106/scalar/v1" -UseBasicParsing -TimeoutSec 2 -ErrorAction SilentlyContinue
        if ($response.StatusCode -eq 200) {
            $serviceReady = $true
        }
    }
    catch {
        $retryCount++
        Write-Host "." -NoNewline -ForegroundColor Yellow
        Start-Sleep -Seconds 2
    }
}

Write-Host ""
Write-Host ""

if ($serviceReady) {
    Write-Host "=== 🎉 服務啟動成功！ ===" -ForegroundColor Green
    Write-Host ""
    Write-Host "📖 Scalar API 文件: " -NoNewline -ForegroundColor Cyan
    Write-Host "http://localhost:5106/scalar/v1" -ForegroundColor White
    Write-Host ""
    Write-Host "🔗 OpenAPI 規格: " -NoNewline -ForegroundColor Cyan
    Write-Host "http://localhost:5106/openapi/v1.json" -ForegroundColor White
    Write-Host ""
    Write-Host "🗄️  PostgreSQL: " -NoNewline -ForegroundColor Cyan
    Write-Host "localhost:5432 (auctiondb/auctionuser/auctionpass)" -ForegroundColor White
    Write-Host ""
    Write-Host "📋 查看日誌: " -NoNewline -ForegroundColor Cyan
    Write-Host "docker-compose logs -f auction-service" -ForegroundColor White
    Write-Host ""
    Write-Host "🛑 停止服務: " -NoNewline -ForegroundColor Cyan
    Write-Host "docker-compose down" -ForegroundColor White
    Write-Host ""
    
    # 自動開啟瀏覽器
    Start-Process "http://localhost:5106/scalar/v1"
}
else {
    Write-Host "❌ 服務啟動失敗" -ForegroundColor Red
    Write-Host ""
    Write-Host "查看錯誤日誌:" -ForegroundColor Yellow
    docker-compose logs auction-service
}
