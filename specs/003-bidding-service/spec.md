# 規格文件:競標服務 (Bidding Service)

**版本**: 1.0  
**狀態**: 待審核  
**最後更新**: 2024-01-15  
**所有者**: 開發團隊

---

## 概述

### 目的
競標服務負責處理所有出價相關的邏輯與出價歷史記錄,確保競標過程的公平性、即時性與一致性。

### 範圍
- 競標出價提交與驗證
- 出價歷史記錄與查詢
- 使用者出價記錄追蹤
- 最高出價查詢與快取
- 競標狀態分析

### 關鍵利益相關者
- **使用者**: 參與競標的買家
- **開發團隊**: 負責實作與維護
- **拍賣服務**: 依賴競標服務進行商品狀態更新
- **系統管理員**: 監控競標系統效能與異常

---

## 目標與成功指標

### 業務目標
1. 提供即時、公平的競標機制
2. 確保高併發環境下出價的正確性
3. 提供完整的出價歷史追蹤功能

### 成功指標
| 指標 | 目標值 | 測量方式 |
|------|--------|----------|
| 出價回應時間 | < 100ms (P95) | APM 監控 |
| 併發正確性 | 100% (無重複得標) | 整合測試 |
| 歷史查詢效能 | < 200ms (P95) | APM 監控 |
| 系統可用性 | > 99.9% | 監控系統 |
| 出價成功率 | > 99% (排除業務規則拒絕) | 應用日誌 |

---

## 功能需求

### 使用者故事

#### US-001: 提交競標出價 (P1)
**作為** 買家  
**我想要** 對商品進行出價  
**以便** 參與競標並嘗試獲得商品

**為何此優先級**: 這是競標系統的核心功能,沒有出價功能就沒有競標機制,是最基礎且必須的功能。

**獨立測試**: 可透過呼叫 `POST /api/bids` API 並驗證出價紀錄是否正確儲存、最高出價是否更新、併發情況下是否只有一筆成功,即可完整測試此功能。

**驗收標準**:
1. **Given** 使用者已登入且商品處於進行中狀態,**When** 提交出價金額大於當前最高出價,**Then** 出價成功並回傳 201 狀態碼與出價資訊
2. **Given** 使用者已登入,**When** 提交出價金額小於等於當前最高出價,**Then** 出價失敗並回傳 400 錯誤與明確訊息
3. **Given** 使用者已登入,**When** 對自己發布的商品出價,**Then** 出價失敗並回傳 403 錯誤
4. **Given** 商品已結束,**When** 提交出價,**Then** 出價失敗並回傳 409 錯誤
5. **Given** 多個使用者同時對同一商品出價,**When** 併發請求發生,**Then** 只有一筆出價成功,其他回傳 409 錯誤
6. **Given** 使用者提交出價,**When** 系統處理出價請求,**Then** 回應時間 < 100ms (P95)

---

#### US-002: 查詢出價歷史 (P1)
**作為** 使用者  
**我想要** 查看特定商品的所有出價紀錄  
**以便** 了解競標過程與價格趨勢

**為何此優先級**: 透明的出價歷史是建立使用者信任的關鍵,讓買家了解競爭程度並做出明智的出價決策。

**獨立測試**: 可透過呼叫 `GET /api/auctions/{id}/bids` API 並驗證回傳的出價紀錄是否依時間排序、分頁是否正確、效能是否達標,即可完整測試此功能。

**驗收標準**:
1. **Given** 商品有多筆出價紀錄,**When** 查詢出價歷史,**Then** 回傳所有出價並依時間倒序排列 (最新在前)
2. **Given** 商品有超過 20 筆出價,**When** 不指定分頁參數,**Then** 預設回傳第 1 頁的 20 筆紀錄
3. **Given** 使用者查詢出價歷史,**When** 請求處理完成,**Then** 回應時間 < 200ms (P95)
4. **Given** 商品尚無出價紀錄,**When** 查詢出價歷史,**Then** 回傳空陣列且 totalCount 為 0

---

#### US-003: 查詢使用者出價記錄 (P2)
**作為** 買家  
**我想要** 查看我所有的出價紀錄  
**以便** 追蹤我參與的競標活動

**為何此優先級**: 這是使用者管理自己競標活動的重要功能,雖不及核心競標流程重要,但對使用者體驗有顯著提升。

**獨立測試**: 可透過呼叫 `GET /api/me/bids` API 並驗證回傳的紀錄是否包含正確的商品資訊、是否標示最高出價狀態、篩選是否正確運作,即可完整測試此功能。

**驗收標準**:
1. **Given** 使用者已登入且有出價紀錄,**When** 查詢我的出價,**Then** 回傳所有我的出價並包含商品標題、狀態、是否為最高出價
2. **Given** 使用者的某筆出價是商品的當前最高出價,**When** 查詢我的出價,**Then** 該紀錄的 `isHighestBid` 為 true
3. **Given** 使用者選擇篩選條件為 "active",**When** 查詢我的出價,**Then** 只回傳商品狀態為進行中的出價紀錄
4. **Given** 使用者選擇篩選條件為 "won",**When** 查詢我的出價,**Then** 只回傳我得標的商品紀錄

---

#### US-004: 查詢最高出價 (P2)
**作為** 使用者  
**我想要** 快速查詢商品的當前最高出價  
**以便** 決定是否繼續參與競標

**為何此優先級**: 這是提升系統效能與使用者體驗的優化功能,雖然可透過出價歷史查詢達成,但專用 API 能大幅提升回應速度。

**獨立測試**: 可透過呼叫 `GET /api/auctions/{id}/highest-bid` API 並驗證回傳的最高出價是否正確、快取是否運作、回應時間是否達標,即可完整測試此功能。

**驗收標準**:
1. **Given** 商品有多筆出價,**When** 查詢最高出價,**Then** 回傳金額最高的出價資訊
2. **Given** 商品尚無出價,**When** 查詢最高出價,**Then** 回傳 404 或 highestBid 為 null
3. **Given** Redis 快取已更新,**When** 查詢最高出價,**Then** 優先從快取讀取,回應時間 < 50ms (P95)
4. **Given** 使用者需要查詢多個商品的最高出價,**When** 呼叫批次查詢 API,**Then** 一次請求回傳所有商品的最高出價

---

#### US-005: 競標狀態分析 (P3)
**作為** 系統管理員  
**我想要** 分析商品的競標活躍度  
**以便** 優化推薦演算法與商品排序

**為何此優先級**: 這是進階的數據分析功能,對核心業務流程影響較小,可在基礎功能完成後再實作。

**獨立測試**: 可透過呼叫 `GET /api/auctions/{id}/stats` API 並驗證統計數據是否正確計算,即可完整測試此功能。

**驗收標準**:
1. **Given** 商品有多筆出價,**When** 查詢競標統計,**Then** 回傳總出價次數、不重複出價者數量、價格成長率
2. **Given** 商品有 10 筆出價來自 5 個不同使用者,**When** 查詢統計,**Then** totalBids 為 10,uniqueBidders 為 5

---

### 邊界情況

- **商品不存在**: 出價時若商品 ID 無效,回傳 404 Not Found
- **Token 過期**: 所有需要認證的 API,若 Token 過期,回傳 401 Unauthorized
- **Auction Service 不可用**: 跨服務呼叫失敗時,使用快取資料或回傳 503 Service Unavailable
- **Redis 故障**: 最高出價查詢降級到資料庫查詢,記錄警告日誌
- **併發衝突**: 樂觀鎖版本衝突時,回傳 409 Conflict 並建議重試
- **超大金額**: 出價金額超過 decimal 範圍,回傳 400 Bad Request
- **分頁邊界**: 查詢頁數超出範圍時,回傳空陣列而非錯誤

---

## 功能需求

### FR-001: 出價提交 API
- **端點**: `POST /api/bids`
- **驗證**: 需要 JWT 認證
- **請求體**:
  ```json
  {
    "auctionId": "string (UUID)",
    "amount": "decimal"
  }
  ```
- **回應**:
  - 201: 出價成功
    ```json
    {
      "bidId": "string (UUID)",
      "auctionId": "string (UUID)",
      "amount": "decimal",
      "bidAt": "ISO8601 datetime"
    }
    ```
  - 400: 出價金額不足 / 對自己商品出價
  - 404: 商品不存在
  - 409: 商品已結束

### FR-002: 出價金額驗證
- 出價金額必須 > 當前最高出價
- 首次出價必須 >= 起標價
- 金額精確度: 小數點後 2 位

### FR-003: 出價者身份驗證
- 從 JWT token 取得使用者 ID
- 跨服務呼叫 Auction Service 取得商品擁有者
- 驗證出價者 != 商品擁有者

### FR-004: 併發控制
- 使用樂觀鎖 (Optimistic Locking) 或分散式鎖
- 確保同一商品同時只有一個出價交易成功
- 失敗的出價回傳 409 Conflict

### FR-005: 出價歷史紀錄
- 所有出價均永久保存
- 包含: bidId, auctionId, bidderId, amount, bidAt, rowVersion
- 索引: auctionId + bidAt (降序)

### FR-006: 查詢出價歷史 API
- **端點**: `GET /api/auctions/{auctionId}/bids`
- **查詢參數**: `page`, `pageSize` (預設 20)
- **回應**:
  ```json
  {
    "items": [
      {
        "bidId": "string",
        "bidderId": "string",
        "amount": "decimal",
        "bidAt": "ISO8601 datetime"
      }
    ],
    "totalCount": "integer",
    "page": "integer",
    "pageSize": "integer"
  }
  ```

### FR-007: 查詢使用者出價記錄 API
- **端點**: `GET /api/me/bids`
- **查詢參數**: `status` (all/active/won/lost), `page`, `pageSize`
- **回應**:
  ```json
  {
    "items": [
      {
        "bidId": "string",
        "auctionId": "string",
        "amount": "decimal",
        "bidAt": "ISO8601 datetime",
        "isHighestBid": "boolean",
        "auctionStatus": "string",
        "auctionTitle": "string"
      }
    ],
    "totalCount": "integer"
  }
  ```

### FR-008: 跨服務資料同步
- 呼叫 Auction Service 取得商品資訊:
  - 商品標題 (用於使用者出價記錄)
  - 商品狀態 (Active/Ended/Sold)
  - 商品擁有者 ID
- 實作快取機制減少跨服務呼叫

### FR-009: 最高出價快取
- 使用 Redis 快取每個商品的最高出價
- Key: `auction:{auctionId}:highest_bid`
- TTL: 商品結束時間 + 1 天
- 出價成功時更新快取

### FR-010: 查詢最高出價 API
- **端點**: `GET /api/auctions/{auctionId}/highest-bid`
- **回應**:
  ```json
  {
    "auctionId": "string",
    "highestBid": {
      "bidId": "string",
      "bidderId": "string",
      "amount": "decimal",
      "bidAt": "ISO8601 datetime"
    }
  }
  ```
- **優先順序**: Redis 快取 → 資料庫查詢

### FR-011: 批次查詢最高出價 API
- **端點**: `POST /api/bids/highest-bids/batch`
- **請求體**:
  ```json
  {
    "auctionIds": ["uuid1", "uuid2", "..."]
  }
  ```
- **回應**: 陣列,每個元素為 FR-010 的回應格式

### FR-012: 競標統計分析 API
- **端點**: `GET /api/auctions/{auctionId}/stats`
- **回應**:
  ```json
  {
    "auctionId": "string",
    "totalBids": "integer",
    "uniqueBidders": "integer",
    "priceGrowth": {
      "startPrice": "decimal",
      "currentHighest": "decimal",
      "growthRate": "decimal"
    }
  }
  ```

### FR-013: 資料庫設計
**Bids 資料表**:
- bidId (UUID, PK)
- auctionId (UUID, FK, indexed)
- bidderId (UUID, indexed)
- amount (decimal)
- bidAt (timestamp, indexed)
- rowVersion (for optimistic locking)
- createdAt (timestamp)

**索引**:
- (auctionId, bidAt DESC): 出價歷史查詢
- (bidderId, bidAt DESC): 使用者出價記錄查詢
- (auctionId, amount DESC): 最高出價查詢

### FR-014: 錯誤處理
- 400 Bad Request: 出價金額不足、驗證失敗
- 401 Unauthorized: Token 無效或過期
- 403 Forbidden: 對自己商品出價
- 404 Not Found: 商品不存在
- 409 Conflict: 商品已結束、併發衝突
- 500 Internal Server Error: 系統異常
- 503 Service Unavailable: 依賴服務不可用

### FR-015: 日誌與監控
- 記錄所有出價嘗試 (成功與失敗)
- 記錄併發衝突次數
- 記錄跨服務呼叫延遲
- 記錄快取命中率
- 使用結構化日誌 (JSON 格式)

### FR-016: 效能優化
- 最高出價查詢優先使用 Redis
- 使用者出價記錄查詢支援索引
- 跨服務呼叫實作快取與超時控制
- 資料庫連線池配置: min 10, max 50

### FR-017: 安全性
- 所有 API 需要 JWT 驗證
- 驗證使用者身份與 Token 中的 UserId 一致
- 防止 SQL Injection (使用參數化查詢)
- 敏感資訊 (如出價者身份) 不記錄於日誌

### FR-018: 可測試性
- 所有業務邏輯封裝於 Service 層
- 資料存取使用 Repository 模式
- 跨服務呼叫使用介面抽象
- 提供 In-Memory 測試替代實作

---

### 關鍵實體

- **Bid (出價)**: 代表一筆競標出價,包含出價者、金額、時間等資訊,與 Auction 為多對一關係
- **Auction (商品)**: 外部實體,由 Auction Service 管理,Bidding Service 透過跨服務呼叫取得商品資訊
- **User (使用者)**: 外部實體,由 Member Service 管理,出價者身份透過 JWT 驗證

---

## 成功標準

### SC-001: 功能完整性
- [ ] 所有 API 端點正確實作並通過整合測試
- [ ] 出價提交、查詢、統計功能完整可用
- [ ] 跨服務呼叫正確實作並有降級機制

### SC-002: 效能達標
- [ ] 出價 API 回應時間 < 100ms (P95)
- [ ] 最高出價查詢 < 50ms (P95)
- [ ] 歷史查詢 < 200ms (P95)
- [ ] Redis 快取命中率 > 90%

### SC-003: 併發正確性
- [ ] 通過 100 併發使用者同時出價的壓力測試
- [ ] 無重複得標者 (同一商品只有一個最高出價)
- [ ] 併發衝突正確回傳 409 錯誤

### SC-004: 資料一致性
- [ ] 所有出價永久保存,無遺失
- [ ] 最高出價快取與資料庫一致
- [ ] 跨服務資料同步正確

### SC-005: 測試覆蓋率
- [ ] 單元測試覆蓋率 > 80%
- [ ] 整合測試覆蓋所有 API 端點
- [ ] 包含併發場景測試

### SC-006: 錯誤處理
- [ ] 所有錯誤情境有對應的 HTTP 狀態碼
- [ ] 錯誤回應包含清楚的訊息
- [ ] 系統異常不暴露內部實作細節

### SC-007: 監控與可觀測性
- [ ] 所有 API 有 APM 追蹤
- [ ] 結構化日誌正確記錄關鍵事件
- [ ] 提供 Health Check 端點

### SC-008: 安全性
- [ ] 通過 OWASP Top 10 安全檢查
- [ ] JWT 驗證正確實作
- [ ] 無 SQL Injection 漏洞

---

## 技術考量

### 架構決策
- **語言/框架**: ASP.NET Core 8
- **資料庫**: PostgreSQL
- **快取**: Redis
- **併發控制**: Entity Framework Core Optimistic Concurrency
- **跨服務通訊**: HTTP REST API (使用 HttpClient)

### 依賴服務
- **Auction Service**: 查詢商品資訊、狀態、擁有者
- **Member Service**: 驗證 JWT Token (間接依賴)

### 限制
- 不支援出價撤回
- 不支援代理出價 (Proxy Bidding)
- 歷史查詢僅支援時間排序,不支援金額排序

---

## 風險與假設

### 風險
- **R-001**: Redis 故障導致最高出價查詢效能下降 → 降級到資料庫查詢
- **R-002**: 高併發導致資料庫鎖競爭 → 監控衝突率,必要時調整鎖策略
- **R-003**: Auction Service 不可用 → 快取商品資訊,設定合理超時

### 假設
- **A-001**: Auction Service 提供查詢商品資訊的 API
- **A-002**: Redis 可用性 > 99.9%
- **A-003**: 出價頻率不超過 1000 次/秒 (單一商品)

---

## 待解決問題
- Q-001: 是否需要實作出價通知功能 (WebSocket/SignalR)?
- Q-002: 商品結束後,如何通知得標者? (跨服務或事件驅動?)
- Q-003: 是否需要實作出價上限 (max bid per user)?

---

**憲法檢查**:
- [x] **原則 I: 程式碼品質優先** - 規格要求測試覆蓋率 > 80%、Repository 模式、介面抽象
- [x] **原則 II: 測試驅動開發** - SC-005 明確要求單元測試與整合測試
- [x] **原則 III: 使用者體驗一致性** - API 回應格式統一、錯誤訊息清楚
- [x] **原則 IV: 效能要求** - 明確定義回應時間目標與快取策略
- [x] **原則 V: 可觀測性** - FR-015 要求結構化日誌、APM 追蹤、Health Check
- [x] **原則 VI: 簡潔性與 YAGNI** - 不實作未明確要求的功能 (如代理出價),專注核心競標邏輯

