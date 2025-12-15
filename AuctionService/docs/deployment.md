# AuctionService 部署指南

## 概述

本指南說明如何在不同環境中部署 AuctionService，包括 Docker 容器化部署、環境變數配置、資料庫遷移以及監控設定。

## 環境需求

### 系統需求
- **作業系統**: Linux/Windows/macOS
- **記憶體**: 至少 2GB RAM
- **儲存空間**: 至少 5GB 可用空間
- **網路**: 支援 HTTP/HTTPS

### 外部依賴
- **PostgreSQL**: 16.0+
- **BiddingService**: 可存取的 HTTP API
- **認證服務**: JWT token 發行服務

## Docker 部署

### 使用 Docker Compose (推薦)

1. **複製專案**:
```bash
git clone https://github.com/your-org/AuctionService.git
cd AuctionService
```

2. **環境配置**:
```bash
# 複製環境檔案
cp .env.example .env

# 編輯環境變數
nano .env
```

3. **啟動服務**:
```bash
# 開發環境
docker-compose up -d

# 生產環境
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

4. **檢查服務狀態**:
```bash
docker-compose ps
docker-compose logs auctionservice
```

### 手動 Docker 部署

1. **建置映像**:
```bash
# 使用 Dockerfile 建置
docker build -t auctionservice:latest .

# 或使用多階段建置
docker build --target production -t auctionservice:prod .
```

2. **運行容器**:
```bash
docker run -d \
  --name auctionservice \
  -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection="Host=auctionservice-db;Database=auctionservice_prod;Username=auctionservice;Password=Dev@Password123" \
  -e BiddingService__BaseUrl="http://bidding-service:8080" \
  auctionservice:latest
```

## 環境變數配置

### 必要環境變數

```bash
# ASP.NET Core 設定
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080

# 資料庫連線
ConnectionStrings__DefaultConnection=Host=auctionservice-db;Port=5432;Database=auctionservice_prod;Username=auctionservice;Password=Dev@Password123;Pooling=true;MinPoolSize=5;MaxPoolSize=100

# JWT 認證 (生產環境)
JWT__Issuer=https://auth.yourdomain.com
JWT__Audience=https://api.yourdomain.com
JWT__SecretKey=your-256-bit-secret-key-here
JWT__ExpiryMinutes=60

# BiddingService 整合
BiddingService__BaseUrl=https://bidding-api.yourdomain.com
BiddingService__TimeoutSeconds=30
BiddingService__RetryCount=3

# 日誌設定
Serilog__MinimumLevel__Default=Information
Serilog__MinimumLevel__Override__Microsoft=Warning
Serilog__WriteTo__0__Name=Console
Serilog__WriteTo__1__Name=File
Serilog__WriteTo__1__Args__path=/app/logs/auction-.log
Serilog__WriteTo__1__Args__rollingInterval=Day

# CORS 設定
CORS__AllowedOrigins=https://yourdomain.com,https://app.yourdomain.com
CORS__AllowedMethods=GET,POST,PUT,DELETE,OPTIONS
CORS__AllowedHeaders=Content-Type,Authorization,X-Correlation-Id

# YARP API Gateway (如果使用)
ReverseProxy__Clusters__auction-cluster__Destinations__destination1__Address=https://api-internal.yourdomain.com:8080
```

### 開發環境變數

```bash
# .env 檔案
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://localhost:5000

ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=auctionservice_dev;Username=auctionservice;Password=Dev@Password123

BiddingService__BaseUrl=http://localhost:5001

# 開發環境簡化設定
JWT__ValidateIssuer=false
JWT__ValidateAudience=false
JWT__ValidateLifetime=true
JWT__ValidateIssuerSigningKey=false
```

## 資料庫設定

### PostgreSQL 初始化

1. **建立資料庫和使用者**:
```sql
-- 連接到 PostgreSQL 作為超級使用者
CREATE DATABASE auction_prod;
CREATE USER auction_user WITH ENCRYPTED PASSWORD 'your_secure_password';
GRANT ALL PRIVILEGES ON DATABASE auction_prod TO auction_user;

-- 切換到新資料庫
\c auction_prod;

-- 授與 schema 權限
GRANT ALL ON SCHEMA public TO auction_user;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO auction_user;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO auction_user;
```

2. **執行資料庫遷移**:
```bash
# 使用 dotnet-ef 工具
dotnet ef database update --project src/AuctionService.Infrastructure --startup-project src/AuctionService.Api

# 或在容器中執行
docker-compose exec auctionservice dotnet ef database update
```

3. **驗證資料庫**:
```bash
# 檢查連線
docker-compose exec postgres psql -U auction_user -d auction_prod -c "SELECT version();"

# 檢查資料表
docker-compose exec postgres psql -U auction_user -d auction_prod -c "\dt"
```

## Kubernetes 部署

### 部署清單

1. **Namespace**:
```yaml
apiVersion: v1
kind: Namespace
metadata:
  name: auction-system
```

2. **ConfigMap**:
```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: auctionservice-config
  namespace: auction-system
data:
  ASPNETCORE_ENVIRONMENT: "Production"
  ASPNETCORE_URLS: "http://+:8080"
  Serilog__MinimumLevel__Default: "Information"
```

3. **Secret**:
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: auctionservice-secrets
  namespace: auction-system
type: Opaque
data:
  # base64 編碼的密碼
  DB_PASSWORD: eW91cl9zZWN1cmVfcGFzc3dvcmQ=
  JWT_SECRET: eW91ci0yNTYtYml0LXNlY3JldC1rZXktaGVyZQ==
```

4. **Deployment**:
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: auctionservice
  namespace: auction-system
spec:
  replicas: 3
  selector:
    matchLabels:
      app: auctionservice
  template:
    metadata:
      labels:
        app: auctionservice
    spec:
      containers:
      - name: auctionservice
        image: your-registry/auctionservice:latest
        ports:
        - containerPort: 8080
        env:
        - name: ASPNETCORE_ENVIRONMENT
          valueFrom:
            configMapKeyRef:
              name: auctionservice-config
              key: ASPNETCORE_ENVIRONMENT
        - name: ConnectionStrings__DefaultConnection
          value: "Host=auctionservice-db;Port=5432;Database=auctionservice_prod;Username=auctionservice;Password=$(DB_PASSWORD)"
        - name: JWT__SecretKey
          value: "$(JWT_SECRET)"
        envFrom:
        - configMapRef:
            name: auctionservice-config
        - secretRef:
            name: auctionservice-secrets
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /api/health
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /api/health
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 5
```

5. **Service**:
```yaml
apiVersion: v1
kind: Service
metadata:
  name: auctionservice-service
  namespace: auction-system
spec:
  selector:
    app: auctionservice
  ports:
  - port: 80
    targetPort: 8080
  type: ClusterIP
```

6. **Ingress**:
```yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: auctionservice-ingress
  namespace: auction-system
  annotations:
    nginx.ingress.kubernetes.io/rewrite-target: /
spec:
  rules:
  - host: api.yourdomain.com
    http:
      paths:
      - path: /api
        pathType: Prefix
        backend:
          service:
            name: auctionservice-service
            port:
              number: 80
```

### 部署指令

```bash
# 應用部署清單
kubectl apply -f k8s/

# 檢查部署狀態
kubectl get pods -n auction-system
kubectl get services -n auction-system
kubectl get ingress -n auction-system

# 查看日誌
kubectl logs -f deployment/auctionservice -n auction-system

# 檢查健康狀態
kubectl exec -it deployment/auctionservice -n auction-system -- curl http://localhost:8080/api/health
```

## 監控與日誌

### 應用程式指標

1. **健康檢查端點**:
```bash
# 應用程式健康狀態
curl https://api.yourdomain.com/api/health

# 詳細健康檢查
curl https://api.yourdomain.com/api/health?detail=true
```

2. **效能指標** (如果啟用):
```bash
# Prometheus 格式指標
curl https://api.yourdomain.com/metrics
```

### 日誌收集

1. **結構化日誌**:
```json
{
  "@timestamp": "2025-12-10T10:00:00.000Z",
  "@level": "Information",
  "MessageTemplate": "BiddingService call: {Endpoint} | Status: {StatusCode} | Duration: {Duration}ms | CorrelationId: {CorrelationId}",
  "Endpoint": "GET /api/bids/auction/123",
  "StatusCode": 200,
  "Duration": 150,
  "CorrelationId": "550e8400-e29b-41d4-a716-446655440000",
  "SourceContext": "AuctionService.Infrastructure.Clients.BiddingServiceClient"
}
```

2. **ELK Stack 整合**:
```yaml
# Filebeat 配置
filebeat.inputs:
- type: log
  paths:
    - /app/logs/*.log
  json.keys_under_root: true
  json.overwrite_keys: true

output.elasticsearch:
  hosts: ["elasticsearch:9200"]
```

### 監控儀表板

建議使用以下監控工具：

- **Prometheus + Grafana**: 應用程式指標和儀表板
- **ELK Stack**: 日誌聚合和分析
- **Jaeger**: 分散式追蹤
- **AlertManager**: 告警管理

## 安全性配置

### HTTPS 設定

1. **使用反向代理**:
```nginx
# Nginx 配置範例
server {
    listen 443 ssl http2;
    server_name api.yourdomain.com;

    ssl_certificate /etc/ssl/certs/yourdomain.crt;
    ssl_certificate_key /etc/ssl/private/yourdomain.key;

    location /api {
        proxy_pass http://localhost:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

2. **直接 HTTPS**:
```bash
# 環境變數
ASPNETCORE_URLS=https://+:443
Kestrel__Certificates__Default__Path=/app/certs/certificate.pfx
Kestrel__Certificates__Default__Password=your_certificate_password
```

### 網路安全

1. **防火牆規則**:
```bash
# UFW 範例
ufw allow 22/tcp
ufw allow 80/tcp
ufw allow 443/tcp
ufw --force enable
```

2. **容器網路**:
```yaml
# Docker Compose 網路隔離
networks:
  auction-network:
    driver: bridge
    internal: true

services:
  auctionservice:
    networks:
      - auction-network
```

## 備份與災難復原

### 資料庫備份

```bash
# 每日備份腳本
#!/bin/bash
BACKUP_DIR="/backups"
DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="$BACKUP_DIR/auction_$DATE.sql"

pg_dump -h postgres -U auction_user -d auction_prod > $BACKUP_FILE

# 壓縮備份
gzip $BACKUP_FILE

# 保留最近 30 天的備份
find $BACKUP_DIR -name "auction_*.sql.gz" -mtime +30 -delete
```

### 應用程式備份

```bash
# 應用程式設定備份
tar -czf config_backup_$(date +%Y%m%d).tar.gz \
  .env \
  docker-compose.yml \
  docker-compose.override.yml \
  k8s/
```

## 疑難排解

### 常見問題

1. **資料庫連線失敗**:
```bash
# 檢查 PostgreSQL 狀態
docker-compose ps postgres

# 檢查連線字串
docker-compose exec auctionservice printenv | grep ConnectionStrings

# 測試連線
docker-compose exec auctionservice dotnet ef database update --dry-run
```

2. **應用程式啟動失敗**:
```bash
# 查看詳細日誌
docker-compose logs auctionservice

# 檢查環境變數
docker-compose exec auctionservice env

# 手動啟動除錯
docker-compose exec auctionservice dotnet run --no-launch-profile
```

3. **健康檢查失敗**:
```bash
# 檢查資料庫連線
docker-compose exec auctionservice curl http://localhost:8080/api/health

# 檢查 BiddingService
curl https://bidding-api.yourdomain.com/api/health
```

### 效能調優

1. **應用程式設定**:
```json
{
  "Kestrel": {
    "Limits": {
      "MaxConcurrentConnections": 100,
      "MaxConcurrentUpgradedConnections": 100
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=auctionservice-db;Port=5432;Database=auctionservice_prod;Username=auctionservice;Password=Dev@Password123;Pooling=true;MinPoolSize=10;MaxPoolSize=200"
  }
}
```

2. **資料庫調優**:
```sql
-- PostgreSQL 配置
ALTER SYSTEM SET shared_buffers = '256MB';
ALTER SYSTEM SET effective_cache_size = '1GB';
ALTER SYSTEM SET work_mem = '4MB';
ALTER SYSTEM SET maintenance_work_mem = '64MB';
```

## 版本升級

### 滾動更新策略

```bash
# Kubernetes 滾動更新
kubectl set image deployment/auctionservice auctionservice=your-registry/auctionservice:v2.0.0
kubectl rollout status deployment/auctionservice

# Docker Compose 更新
docker-compose pull
docker-compose up -d --no-deps auctionservice
```

### 資料庫遷移

```bash
# 建立新遷移
dotnet ef migrations add UpgradeToV2 --project src/AuctionService.Infrastructure

# 應用遷移
dotnet ef database update

# 驗證資料完整性
# 檢查資料遷移腳本
```

## 聯絡資訊

部署相關問題請聯絡：
- **DevOps 團隊**: devops@auctionservice.com
- **技術支援**: support@auctionservice.com
- **緊急聯絡**: emergency@auctionservice.com