# 快速啟動指南：AuctionService 後端骨架

**功能分支**: `001-backend-scaffold`  
**目標**: 從 clone 到第一次成功啟動服務，時間 < 10 分鐘（SC-001）

---

## 前置需求

| 工具 | 版本需求 | 驗證指令 |
|------|---------|---------|
| .NET SDK | 10.0+ | `dotnet --version` |
| Docker Desktop | 4.0+ | `docker --version` |
| Git | 2.x+ | `git --version` |

---

## 步驟 1：Clone 並切換分支

```bash
git clone <repository-url> AuctionService
cd AuctionService
git checkout 001-backend-scaffold
```

---

## 步驟 2：設定環境變數

複製範本並填入你的設定值：

```bash
cp .env.example .env
```

編輯 `.env` 填入以下必要值：

```env
# 資料庫
POSTGRES_USER=auctionservice
POSTGRES_PASSWORD=your-strong-password-here

# pgAdmin（可選）
PGADMIN_EMAIL=admin@example.com
PGADMIN_PASSWORD=admin-password

# JWT（至少 32 字元的隨機字串）
JWT_SECRET=your-32-character-minimum-secret-key-here
JWT_ISSUER=AuctionService
JWT_AUDIENCE=AuctionServiceClient
JWT_EXPIRY_MINUTES=60
```

> **⚠️ 安全性提醒**: `JWT_SECRET` 必須至少 32 字元，切勿使用預設值或提交至版本控制。

---

## 步驟 3：啟動資料庫

```bash
docker compose up -d
```

驗證容器已啟動：

```bash
docker compose ps
# 預期輸出：postgres 和 pgadmin 狀態均為 running
```

---

## 步驟 4：執行資料庫 Migration

依序對各模組執行初始 Migration：

```bash
# Member 模組
dotnet ef database update \
  --project src/Modules/Member/Member.csproj \
  --startup-project src/AuctionService.Api/AuctionService.Api.csproj

# Auction 模組
dotnet ef database update \
  --project src/Modules/Auction/Auction.csproj \
  --startup-project src/AuctionService.Api/AuctionService.Api.csproj

# Bidding 模組
dotnet ef database update \
  --project src/Modules/Bidding/Bidding.csproj \
  --startup-project src/AuctionService.Api/AuctionService.Api.csproj

# Ordering 模組
dotnet ef database update \
  --project src/Modules/Ordering/Ordering.csproj \
  --startup-project src/AuctionService.Api/AuctionService.Api.csproj

# Notification 模組
dotnet ef database update \
  --project src/Modules/Notification/Notification.csproj \
  --startup-project src/AuctionService.Api/AuctionService.Api.csproj
```

---

## 步驟 5：建置並啟動 API

```bash
# 建置（驗證零編譯錯誤，對應 SC-002）
dotnet build

# 啟動 API
dotnet run --project src/AuctionService.Api/AuctionService.Api.csproj
```

---

## 步驟 6：驗證服務正常

### 開啟 Swagger UI

瀏覽至 [https://localhost:5001/swagger](https://localhost:5001/swagger)

預期結果：Swagger UI 顯示所有模組端點

### 健康檢查

```bash
curl https://localhost:5001/health
```

預期回應（200 OK）：
```json
{
  "status": "Healthy",
  "duration": "00:00:00.0045678",
  "entries": {
    "member-db": { "status": "Healthy" },
    "auction-db": { "status": "Healthy" },
    "bidding-db": { "status": "Healthy" },
    "ordering-db": { "status": "Healthy" },
    "notification-db": { "status": "Healthy" }
  }
}
```

---

## 步驟 7：執行測試

```bash
dotnet test
```

預期結果：全部測試通過（SC-003）

---

## 疑難排解

### 問題：`dotnet run` 報告缺少環境變數

**原因**：`.env` 未正確設定，或必要設定項目缺失  
**解決**：服務採用 fail-fast 設計，啟動時會明確報告缺少的設定項目名稱，依提示填入 `.env`

### 問題：`dotnet ef database update` 失敗

**原因**：資料庫容器尚未完全就緒，或連線字串設定有誤  
**解決**：
1. 確認 `docker compose ps` 顯示 postgres 為 `healthy`
2. 確認 `.env` 中 `POSTGRES_USER`、`POSTGRES_PASSWORD` 與 `appsettings.Development.json` 一致

### 問題：Swagger 憑證警告

**原因**：開發環境使用自簽憑證  
**解決**：
```bash
dotnet dev-certs https --trust
```

### 問題：pgAdmin 無法連線

**原因**：pgAdmin 初次啟動較慢  
**解決**：等待約 30 秒後，瀏覽至 [http://localhost:5050](http://localhost:5050)，使用 `.env` 中設定的帳密登入

---

## 開發工具連結（啟動後）

| 服務 | URL |
|------|-----|
| Swagger UI | https://localhost:5001/swagger |
| 健康檢查 | https://localhost:5001/health |
| pgAdmin | http://localhost:5050 |

---

## Migration 新增流程（開發中使用）

當你修改了某模組的 Entity 後，執行以下指令新增 Migration：

```bash
# 以 Member 模組為例
dotnet ef migrations add <MigrationName> \
  --project src/Modules/Member/Member.csproj \
  --startup-project src/AuctionService.Api/AuctionService.Api.csproj \
  --output-dir Infrastructure/Persistence/Migrations
```
