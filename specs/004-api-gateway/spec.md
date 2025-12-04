# 規格文件：API 閘道器 (API Gateway)

**版本**: 1.0  
**狀態**: 待審核  
**最後更新**: 2025-11-05  
**所有者**: 開發團隊

---

## 澄清事項

### Session 2025-11-07

- Q: 多實例部署時是否使用 Redis 作為 Rate Limit 計數器? → A: 單獨使用 Redis (適合多實例部署,計數集中管理,支援水平擴展)
- Q: JWT 使用哪種演算法 (HS256 對稱金鑰 vs RS256 非對稱金鑰)? → A: HS256 (對稱金鑰,HMAC-SHA256,驗證快速,適合內部微服務)
- Q: 當 Redis 不可用時,API Gateway 應該如何處理 Rate Limiting? → A: 暫時允許所有請求通過並記錄告警 (優先保證可用性,犧牲短期限流功能)
- Q: 是否需要支援 API 版本控制 (如 `/v1/api/members/**`)? → A: 延後決定 (先不實作,未來需要時再加)
- Q: 是否需要實作 API 使用量統計與分析功能? → A: 僅記錄基本統計資料 (請求數、錯誤率、延遲,透過現有日誌系統與 APM 工具)
- Q: 後端服務發現機制要使用靜態設定檔還是動態服務發現? → A: 靜態設定檔 (appsettings.json),但需做好服務發現抽象以便未來遷移到 Consul
- Q: 請求聚合時,呼叫多個後端服務是並行還是序列執行? → A: 並行呼叫 (使用 Task.WhenAll 同時呼叫所有服務,總延遲 = 最慢的服務)

---

## 概述

### 目的
API 閘道器作為所有客戶端請求的統一入口,負責請求路由、身份驗證、請求聚合、錯誤處理與安全防護,簡化客戶端與後端微服務的互動。

### 範圍
- 請求路由至對應的後端微服務
- JWT 身份驗證與授權
- 請求聚合減少客戶端往返次數
- 統一錯誤處理與訊息轉換
- 限流與安全防護 (Rate Limiting, CORS, SSL)

### 關鍵利益相關者
- **客戶端開發者**: 透過單一入口存取所有服務
- **後端服務**: 減輕認證與安全負擔
- **系統管理員**: 集中監控與流量控制
- **使用者**: 獲得更快速、更安全的服務體驗

---

## 目標與成功指標

### 業務目標
1. 提供統一的 API 入口，簡化客戶端開發
2. 集中處理認證與安全，提升系統安全性
3. 優化請求處理，提升使用者體驗

### 成功指標
| 指標 | 目標值 | 測量方式 |
|------|--------|----------|
| 路由延遲 | < 10ms (P95) | APM 監控 |
| JWT 驗證延遲 | < 20ms (P95) | APM 監控 |
| 請求聚合回應時間 | < 300ms (P95) | APM 監控 |
| 系統可用性 | > 99.9% | 監控系統 |
| 錯誤處理覆蓋率 | 100% | 測試覆蓋率 |
| Rate Limit 準確性 | 100% (無誤判) | 整合測試 |

---

## 功能需求

### 使用者故事

#### US-001: 請求路由 (P1)
**作為** 客戶端開發者  
**我想要** 透過單一 API 入口存取所有後端服務  
**以便** 簡化 API 呼叫邏輯並統一錯誤處理

**為何此優先級**: 這是 API Gateway 的核心功能，沒有路由功能就無法將請求分發到正確的服務，是整個系統運作的基礎。

**獨立測試**: 可透過向不同路徑發送請求 (如 `/api/members/*`, `/api/auctions/*`, `/api/bids/*`) 並驗證請求是否正確轉發到對應服務，即可完整測試此功能。

**驗收標準**:
1. **Given** 客戶端發送請求到 `/api/members/*`,**When** Gateway 接收請求,**Then** 請求被路由到 Member Service
2. **Given** 客戶端發送請求到 `/api/auctions/*`,**When** Gateway 接收請求,**Then** 請求被路由到 Auction Service
3. **Given** 客戶端發送請求到 `/api/bids/*`,**When** Gateway 接收請求,**Then** 請求被路由到 Bidding Service
4. **Given** 客戶端發送請求到 `/api/me/bids`,**When** Gateway 接收請求,**Then** 請求被路由到 Bidding Service
5. **Given** 客戶端發送請求到 `/api/me/follows`,**When** Gateway 接收請求,**Then** 請求被路由到 Auction Service
6. **Given** 客戶端發送請求到 `/api/me` (GET/PUT),**When** Gateway 接收請求,**Then** 請求被路由到 Member Service
7. **Given** 路由處理完成,**When** 測量延遲,**Then** 路由延遲 < 10ms (P95)

---

#### US-002: JWT 身份驗證 (P1)
**作為** 系統管理員  
**我想要** Gateway 統一處理 JWT 驗證  
**以便** 後端服務專注業務邏輯，提升安全性

**為何此優先級**: 身份驗證是保護系統資源的第一道防線，必須在路由之前完成，與路由功能同等重要。

**獨立測試**: 可透過發送帶有有效/無效/過期 JWT 的請求到需認證端點，並驗證 Gateway 是否正確接受或拒絕請求，即可完整測試此功能。

**驗收標準**:
1. **Given** 客戶端請求需認證端點且提供有效 JWT,**When** Gateway 驗證 Token,**Then** 請求通過並轉發到後端服務
2. **Given** 客戶端請求需認證端點但未提供 JWT,**When** Gateway 檢查請求,**Then** 回傳 401 Unauthorized 且不轉發請求
3. **Given** 客戶端提供已過期的 JWT,**When** Gateway 驗證 Token,**Then** 回傳 401 Unauthorized 並包含 "Token expired" 訊息
4. **Given** 客戶端提供無效的 JWT (簽章錯誤),**When** Gateway 驗證 Token,**Then** 回傳 401 Unauthorized 並包含 "Invalid token" 訊息
5. **Given** 客戶端請求公開端點 (登入/註冊/商品清單/商品詳細),**When** Gateway 檢查端點,**Then** 不需驗證 JWT 直接轉發
6. **Given** JWT 驗證通過,**When** 轉發請求到後端,**Then** 將使用者資訊 (UserId) 加入請求標頭
7. **Given** JWT 驗證處理完成,**When** 測量延遲,**Then** 驗證延遲 < 20ms (P95)

---

#### US-003: 統一錯誤處理 (P2)
**作為** 客戶端開發者  
**我想要** Gateway 統一處理後端錯誤並轉換為友善訊息  
**以便** 提供一致的錯誤回應格式給使用者

**為何此優先級**: 錯誤處理提升使用者體驗，但系統在無此功能時仍可運作 (客戶端直接處理後端錯誤)，因此優先級次於核心路由與認證。

**獨立測試**: 可透過觸發各種後端錯誤 (404, 500, 503) 並驗證 Gateway 是否將錯誤轉換為統一格式且記錄日誌，即可完整測試此功能。

**驗收標準**:
1. **Given** 後端服務回傳 404 Not Found,**When** Gateway 接收回應,**Then** 回傳統一格式錯誤訊息給客戶端
2. **Given** 後端服務回傳 500 Internal Server Error,**When** Gateway 接收回應,**Then** 隱藏內部錯誤細節並回傳 "服務暫時無法使用" 訊息
3. **Given** 後端服務無回應或超時,**When** Gateway 等待逾時,**Then** 回傳 503 Service Unavailable 並記錄錯誤日誌
4. **Given** 任何錯誤發生,**When** Gateway 處理錯誤,**Then** 記錄錯誤日誌包含請求路徑、時間戳、錯誤類型、使用者 ID (如有)
5. **Given** 所有已知錯誤情境,**When** 執行測試,**Then** 錯誤處理覆蓋率達 100%

---

#### US-004: 請求聚合 (P2)
**作為** 客戶端開發者  
**我想要** Gateway 能組合多個服務的資料  
**以便** 減少客戶端的多次請求，提升效能

**為何此優先級**: 請求聚合是效能優化功能，系統在無此功能時仍可正常運作，客戶端可自行發送多次請求，因此為次要優先級。

**獨立測試**: 可透過呼叫聚合端點 (如商品詳細頁) 並驗證回應是否包含來自多個服務的完整資料 (商品+賣家+出價)，即可完整測試此功能。

**驗收標準**:
1. **Given** 客戶端請求商品詳細頁聚合端點,**When** Gateway 處理請求,**Then** 並行呼叫 Auction Service (商品資訊)、Member Service (賣家資訊)、Bidding Service (出價歷史)
2. **Given** 所有服務回應成功,**When** Gateway 組合資料,**Then** 回傳包含商品、賣家、出價的完整資料
3. **Given** 其中一個服務失敗,**When** Gateway 組合資料,**Then** 回傳部分資料並在 metadata.dataAvailability 標註哪些資料不可用
4. **Given** 聚合請求處理完成,**When** 測量延遲,**Then** 回應時間 < 300ms (P95)
5. **Given** 並行呼叫多個服務,**When** 驗證執行方式,**Then** 總延遲應接近最慢服務的延遲,而非所有服務延遲總和

---

#### US-005: 限流與安全防護 (P1)
**作為** 系統管理員  
**我想要** Gateway 實作 Rate Limiting 與安全防護  
**以便** 防止 API 濫用與惡意攻擊

**為何此優先級**: 安全防護是保護系統資源的關鍵功能，與認證同等重要，能防止系統因濫用而崩潰。

**獨立測試**: 可透過在短時間內發送大量請求並驗證 Gateway 是否正確限流、設定 CORS、處理 HTTPS，即可完整測試此功能。

**驗收標準**:
1. **Given** 同一 IP 在 1 分鐘內發送 100 次請求,**When** 發送第 101 次請求,**Then** 回傳 429 Too Many Requests
2. **Given** Rate Limit 觸發,**When** 等待 1 分鐘後,**Then** 限制重置，可繼續發送請求
3. **Given** 客戶端發送跨域請求,**When** Gateway 檢查 CORS,**Then** 正確設定 CORS 標頭允許或拒絕請求
4. **Given** 客戶端發送 HTTP 請求,**When** Gateway 處理請求,**Then** 執行 SSL 終止並使用 HTTPS 通訊
5. **Given** Rate Limit 機制運作,**When** 測試驗證,**Then** 準確性達 100% (無誤判或漏判)

---

### 邊界情況

- **後端服務全部不可用**: Gateway 回傳 503 Service Unavailable 並記錄告警日誌
- **JWT 格式錯誤**: 回傳 401 Unauthorized 並包含 "Malformed token" 訊息
- **超大請求 Payload**: 實作請求大小限制 (如 10MB)，超過則回傳 413 Payload Too Large
- **路由規則衝突**: 優先匹配更具體的路徑 (如 `/api/me/bids` 優先於 `/api/me/*`)
- **聚合請求部分失敗**: 回傳可用的部分資料並標註失敗的服務，而非完全失敗
- **Rate Limit 邊界**: 100 次請求的計數在分鐘邊界時正確重置 (避免計數累積)
- **循環依賴**: 防止 Gateway 呼叫後端服務時產生無限循環 (設定最大重試次數)
- **空 JWT**: 視為未提供 Token，回傳 401 Unauthorized

---

## 功能需求

### FR-001: 路由對應表
Gateway 必須維護以下路由對應規則:
- `/api/members/**` → Member Service
- `/api/auctions/**` → Auction Service
- `/api/bids/**` → Bidding Service
- `/api/me/bids` → Bidding Service (優先匹配)
- `/api/me/follows` → Auction Service (優先匹配)
- `/api/me` (GET/PUT) → Member Service
- 路由規則支援路徑參數與查詢字串透傳

### FR-002: 公開與私有端點清單
- **公開端點 (無需 JWT)**:
  - `POST /api/members/register`
  - `POST /api/members/login`
  - `POST /api/members/refresh-token`
  - `GET /api/auctions` (商品清單)
  - `GET /api/auctions/{id}` (商品詳細)
  - `GET /api/members/{id}` (使用者公開資料)
- **私有端點 (需要 JWT)**:
  - 所有其他端點 (出價、追蹤、個人資料更新等)

### FR-003: JWT 驗證機制
- 從 HTTP 標頭 `Authorization: Bearer <token>` 提取 JWT
- 使用 HS256 演算法驗證 JWT 簽章 (HMAC-SHA256 對稱金鑰)
- 驗證有效期限 (exp claim)
- 驗證失敗回傳 401 Unauthorized 並包含明確錯誤訊息
- 驗證成功後提取 UserId 並加入請求標頭 `X-User-Id` 傳遞給後端服務
- JWT 密鑰透過環境變數或密鑰管理服務取得,不寫入程式碼或設定檔

### FR-004: 錯誤回應格式
所有錯誤回應遵循統一格式:
```json
{
  "error": {
    "code": "string (錯誤代碼)",
    "message": "string (使用者友善訊息)",
    "details": "string (選用，額外說明)",
    "timestamp": "ISO8601 datetime",
    "path": "string (請求路徑)"
  }
}
```

### FR-005: 請求聚合端點
- **商品詳細頁聚合**: `GET /api/aggregated/auctions/{id}`
  - 組合來源:
    - Auction Service: 商品資訊
    - Member Service: 賣家資訊
    - Bidding Service: 出價歷史 (最近 10 筆)
  - **執行方式**: 並行呼叫所有後端服務 (使用 Task.WhenAll),總延遲取決於最慢的服務
  - 回應格式:
    ```json
    {
      "auction": { ... },
      "seller": { ... },
      "bids": [ ... ],
      "metadata": {
        "dataAvailability": {
          "auction": true,
          "seller": true,
          "bids": true
        }
      }
    }
    ```
  - **錯誤處理**: 其中一個或多個服務失敗時,仍回傳可用的部分資料,並在 metadata.dataAvailability 標註哪些資料不可用

### FR-006: Rate Limiting 規則
- **全域限制**: 每個 IP 每分鐘最多 100 次請求
- **識別方式**: 優先使用 `X-Forwarded-For` 標頭，否則使用連線 IP
- **回應**: 超過限制回傳 429 Too Many Requests 並包含 `Retry-After` 標頭
- **計數重置**: 每分鐘的第 0 秒重置計數
- **儲存機制**: 使用 Redis 集中管理計數器,支援多實例部署
  - Redis Key 格式: `ratelimit:{ip}:{timestamp_minute}`
  - 使用 Redis INCR 原子操作確保計數準確性
  - 設定 TTL 為 60 秒自動清理過期計數
- **降級策略**: Redis 不可用時,暫時允許所有請求通過並記錄告警 (優先保證系統可用性 > 99.9%)

### FR-007: CORS 設定
- 允許的來源: 可設定允許的網域清單 (如 `https://example.com`)
- 允許的方法: GET, POST, PUT, DELETE, OPTIONS
- 允許的標頭: Authorization, Content-Type, X-Requested-With
- 憑證支援: `Access-Control-Allow-Credentials: true`

### FR-008: SSL/TLS 終止
- Gateway 處理 HTTPS 連線
- 後端服務可使用 HTTP 通訊 (內部網路)
- 強制客戶端使用 HTTPS (重定向 HTTP 到 HTTPS)

### FR-009: 請求/回應轉換
- 保留原始請求的 HTTP 方法、標頭、查詢字串、請求體
- 加入額外標頭:
  - `X-User-Id`: JWT 驗證通過後的使用者 ID
  - `X-Forwarded-For`: 客戶端 IP
  - `X-Gateway-Request-Id`: 請求追蹤 ID (UUID)

### FR-010: 超時處理
- 後端服務呼叫超時時間: 30 秒
- 聚合請求超時時間: 30 秒 (並行呼叫，總延遲取決於最慢的服務，所有呼叫共用 30 秒超時限制)
- 超時後回傳 504 Gateway Timeout

### FR-011: 健康檢查
- **端點**: `GET /health`
- **回應**:
  ```json
  {
    "status": "healthy|degraded|unhealthy",
    "timestamp": "ISO8601 datetime",
    "services": {
      "memberService": "healthy|unhealthy",
      "auctionService": "healthy|unhealthy",
      "biddingService": "healthy|unhealthy"
    }
  }
  ```
- 定期檢查後端服務健康狀態 (每 30 秒)

### FR-012: 日誌與監控
- 記錄所有請求: 路徑、方法、狀態碼、延遲、使用者 ID、IP
- 記錄錯誤: 錯誤類型、後端服務、錯誤訊息、堆疊追蹤 (不含敏感資訊)
- 記錄 Rate Limit 觸發事件
- 記錄 Redis 降級事件 (Redis 不可用時觸發告警)
- 使用結構化日誌 (JSON 格式)
- 提供 APM 追蹤支援 (分散式追蹤)
- 透過 APM 工具 (如 Application Insights) 提供基本統計分析:
  - 請求數 (按端點、時段統計)
  - 錯誤率 (按端點、狀態碼統計)
  - 延遲分佈 (P50/P95/P99)
  - 不實作專門的統計分析系統 (使用現有日誌與 APM 工具即可滿足基本監控需求)

### FR-013: 請求大小限制
- 請求體最大大小: 10MB
- 超過限制回傳 413 Payload Too Large

### FR-014: 重試機制
- 對後端服務的請求不自動重試 (避免重複操作，如出價)
- 僅健康檢查失敗時標註服務為 unhealthy，不影響實際請求路由

### FR-015: 可測試性
- 所有路由邏輯可透過設定檔配置 (不寫死在程式碼)
- JWT 驗證邏輯使用介面抽象 (可替換為測試用 Mock)
- 後端服務呼叫使用 HTTP Client 介面 (可注入測試替代實作)
- **服務發現使用介面抽象** (IServiceDiscovery),初期實作使用靜態設定檔,未來可無痛切換到 Consul 等動態服務發現機制

---

### 關鍵實體

- **Route (路由規則)**: 定義路徑模式與對應的後端服務，包含是否需要認證的設定
- **RateLimitCounter (限流計數器)**: 追蹤每個 IP 的請求次數與重置時間
- **ServiceHealthStatus (服務健康狀態)**: 記錄每個後端服務的健康狀態與最後檢查時間

---

## 成功標準

### SC-001: 功能完整性
- [ ] 所有路由規則正確實作並通過整合測試
- [ ] JWT 驗證覆蓋所有私有端點
- [ ] 公開端點無需認證即可存取
- [ ] 請求聚合功能正確組合多服務資料

### SC-002: 效能達標
- [ ] 路由延遲 < 10ms (P95)
- [ ] JWT 驗證延遲 < 20ms (P95)
- [ ] 請求聚合回應時間 < 300ms (P95)
- [ ] 系統可用性 > 99.9%

### SC-003: 安全性
- [ ] JWT 驗證正確實作，無繞過漏洞
- [ ] Rate Limiting 準確性 100% (無誤判)
- [ ] CORS 設定正確，無跨域安全問題
- [ ] HTTPS 強制啟用，無 HTTP 明文傳輸
- [ ] 錯誤訊息不暴露內部實作細節

### SC-004: 錯誤處理
- [ ] 錯誤處理覆蓋率 100%
- [ ] 所有錯誤回應格式統一
- [ ] 後端服務失敗時正確降級或回傳錯誤
- [ ] 超時情境正確處理

### SC-005: 可觀測性
- [ ] 所有請求有完整日誌記錄
- [ ] APM 追蹤正確實作
- [ ] 健康檢查端點正常運作
- [ ] 提供請求追蹤 ID 供問題排查

### SC-006: 測試覆蓋率
- [ ] 單元測試覆蓋率 > 80%
- [ ] 整合測試覆蓋所有路由規則
- [ ] 包含 Rate Limiting、JWT 驗證、錯誤處理的測試

### SC-007: 使用者體驗
- [ ] 客戶端開發者可透過單一入口存取所有服務
- [ ] 錯誤訊息清楚且友善
- [ ] 請求聚合功能減少客戶端網路往返次數

---

## 技術考量

### 架構決策
- **語言/框架**: ASP.NET Core 10 (或其他高效能 Gateway 解決方案如 Ocelot、YARP)
- **JWT 驗證**: 使用 HS256 對稱金鑰演算法 (HMAC-SHA256),密鑰透過環境變數管理
- **Rate Limiting**: Redis 集中管理計數器 (支援多實例部署,使用 INCR 原子操作)
- **後端服務發現**: 靜態設定檔 (appsettings.json),透過 IServiceDiscovery 介面抽象,未來可遷移到 Consul

### 依賴服務
- **Member Service**: 路由 `/api/members/**`，提供使用者資料
- **Auction Service**: 路由 `/api/auctions/**`，提供商品資料
- **Bidding Service**: 路由 `/api/bids/**`，提供出價資料
- **Redis**: Rate Limiting 計數器儲存,需 99.9% 可用性

### 限制
- 不支援 WebSocket 路由 (僅 HTTP/HTTPS)
- 請求聚合僅支援預定義的聚合端點，不支援動態組合
- Rate Limiting 基於 IP，無法區分同一 IP 的不同使用者
- 暫不支援 API 版本控制 (未來可擴展)
- 不實作專門的 API 使用量統計分析系統 (透過現有 APM 工具滿足基本監控需求)

---

## 風險與假設

### 風險
- **R-001**: 後端服務全部不可用導致 Gateway 無法提供服務 → 實作健康檢查與告警
- **R-002**: Redis 不可用導致 Rate Limiting 失效 → 降級策略: 暫時允許所有請求通過並記錄告警,運維團隊需監控 Redis 健康狀態並快速恢復
- **R-003**: JWT 金鑰洩漏導致安全漏洞 → 定期輪換金鑰，使用金鑰管理服務
- **R-004**: Redis 降級期間可能遭受 DDoS 攻擊 → 依賴上游防護 (CDN/WAF) 或快速恢復 Redis
- **R-005**: 未來新增 API 版本控制需要重構路由規則與客戶端 → 記錄為技術債,需要時評估遷移成本

### 假設
- **A-001**: 所有後端服務提供 RESTful HTTP API
- **A-002**: JWT 使用 HS256 演算法,所有微服務共享相同密鑰
- **A-003**: 客戶端請求頻率不超過 100 次/分鐘/IP (正常使用情境)
- **A-004**: 後端服務網址相對穩定 (使用靜態設定檔,服務位址變更頻率低)

---

## 待解決問題
- ~~Q-001: 是否需要支援 API 版本控制 (如 `/v1/api/members/**`)?~~ (已解決: 延後決定,先不實作)
- ~~Q-002: 是否需要實作 API 使用量統計與分析功能?~~ (已解決: 透過現有日誌與 APM 工具提供基本統計)
- ~~Q-003: 多實例部署時是否使用 Redis 作為 Rate Limit 計數器?~~ (已解決: 使用 Redis 集中管理)

---

**憲法檢查**:
- [x] **原則 I: 程式碼品質優先** - FR-015 要求介面抽象、可測試性
- [x] **原則 II: 測試驅動開發** - SC-006 明確要求單元測試覆蓋率 > 80%、整合測試
- [x] **原則 III: 使用者體驗一致性** - 統一錯誤格式、友善訊息、單一入口
- [x] **原則 IV: 效能要求** - 明確定義路由 < 10ms、JWT < 20ms、聚合 < 300ms
- [x] **原則 V: 可觀測性** - FR-012 要求結構化日誌、APM 追蹤、健康檢查
- [x] **原則 VI: 簡潔性與 YAGNI** - 不實作未要求的功能 (如 WebSocket、動態聚合)，專注核心 Gateway 職責
