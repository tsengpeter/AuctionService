# 規格文件:競標服務 (Bidding Service)

**版本**: 1.0  
**狀態**: 待審核  
**最後更新**: 2024-01-15  
**所有者**: 開發團隊

---

## 澄清事項

### Session 2025-11-06

- Q1: 併發控制機制的具體實作策略? → A: 選項 E - Redis 作為主要寫入層 + 非同步批次回寫資料庫
  - 出價先寫入 Redis (使用 ZADD 等原子操作確保併發安全)
  - 立即回應使用者 (< 10ms 回應時間)
  - 背景 Worker 每秒批次讀取 Redis 中的新出價並寫入 PostgreSQL
  - Redis 啟用 AOF 持久化 (每秒 fsync) 防止資料遺失
  - 降級策略: Redis 故障時直接寫 DB,犧牲效能保證可用性
  - 未來可平滑演進到 Kafka 事件流架構 (選項 F)

- Q2: Redis 故障時的降級策略細節? → A: 選項 B - 自動降級 + 自動恢復 + 監控告警
  - 健康檢查: 每 10 秒執行一次 Redis PING 命令
  - 降級觸發: 連續 3 次健康檢查失敗,自動切換到 PostgreSQL 直接寫入模式
  - 降級期間: 出價直接寫入 DB (使用樂觀鎖),回應時間降級到 < 100ms
  - 自動恢復: Redis 恢復後,健康檢查連續 5 次成功,自動切回 Redis 寫入模式
  - 監控告警: 降級與恢復事件發送告警通知 (Email/Slack),記錄詳細日誌
  - 避免抖動: 設定冷卻期 30 秒,防止頻繁切換

- Q3: 背景 Worker 失敗重試與資料恢復策略? → A: 選項 B - 指數退避重試 + 死信佇列 + 人工介入
  - 重試策略: 指數退避 (1秒、2秒、4秒),最多重試 3 次
  - 失敗處理: 3 次重試後仍失敗,將出價 ID 移入 Redis `dead_letter_bids` Set
  - 死信佇列: 記錄 bidId、失敗原因、時間戳、重試次數
  - 告警機制: 死信佇列有新項目時,發送告警通知運維團隊
  - 人工介入: 提供管理 API 查詢死信佇列、手動重試、標記為已處理
  - 資料保護: Redis AOF 確保重啟後死信佇列不遺失

- Q4: 查詢出價歷史時的資料來源優先順序? → A: 選項 B - 優先 Redis,無資料才查 PostgreSQL
  - 查詢邏輯: 先從 Redis Sorted Set 查詢該商品的出價記錄
  - Redis 有資料: 直接回傳 (表示尚未同步或部分同步中)
  - Redis 無資料: 查詢 PostgreSQL (表示已完全同步,Redis 已清除)
  - 資料生命週期: 出價寫入 Redis → 背景任務寫入 DB → Redis 清除該筆 (或標記為 processed)
  - 商品結束後: Redis 保留 7 天後過期,之後僅從 PostgreSQL 查詢
  - 最高出價查詢: 同樣邏輯,優先 Redis Hash,無資料才查 DB

- Q5: 使用者出價記錄查詢的資料來源策略? → A: 選項 C - 統一從 PostgreSQL 查詢
  - 查詢「我的出價記錄」API 統一從 PostgreSQL 查詢
  - 理由: 需要跨多個商品查詢,且需要商品資訊(標題、狀態),DB 查詢更高效
  - 接受延遲: 最新出價可能有最多 1 秒的同步延遲 (背景 Worker 執行週期)
  - 簡化邏輯: 避免遍歷多個商品的 Redis 資料,降低複雜度
  - 查詢效能: 利用 (bidderId, bidAt DESC) 索引,確保 < 200ms 回應時間

- Q6: 出價通知功能的實作策略? → A: 選項 B - 前端輪詢 (保留未來 WebSocket 升級路徑)
  - 不實作 WebSocket/SignalR 推送,採用前端輪詢機制
  - 前端使用 `GET /api/auctions/{id}/highest-bid` API,每 3-5 秒輪詢一次
  - 理由: 簡化架構,避免引入額外基礎設施 (SignalR、Redis Pub/Sub)
  - 可接受性: 3-5 秒延遲對拍賣場景可接受 (非即時聊天)
  - 未來演進: API 設計保持不變,可在不破壞現有功能下加入 WebSocket 推送
  - 效能考量: Redis 快取確保輪詢查詢 < 50ms,不造成伺服器負擔

- Q7: 商品結束後得標者通知的職責劃分? → A: 選項 C - Auction Service 負責協調,Bidding Service 僅提供查詢
  - Bidding Service 職責: 僅提供 `GET /api/auctions/{id}/highest-bid` API 供查詢得標者
  - Auction Service 職責: 商品結束時呼叫 Bidding Service 查詢得標者,取得 bidderId
  - 通知職責: Auction Service 將得標資訊傳遞給 Notification Service (未來新增服務)
  - 職責劃分: Bidding Service 專注競標邏輯,不涉及通知或商品狀態管理
  - 解耦設計: 各服務職責單一,未來 Notification Service 統一處理所有通知 (Email/推播/簡訊)

### Session 2025-11-17

- Q: Bid (出價) 的主鍵 (ID) 使用何種生成方式? → A: 雪花ID (Snowflake ID, 64-bit Long)，與 Member Service 及 Auction Service 保持一致，使用 IdGen 或 Snowflake.Core 套件生成
  - 實作機制: 使用 IdGen 或 Snowflake.Core 套件生成 64-bit Long 型態的唯一 ID
  - 配置要求: 必須配置 Worker ID 與 Datacenter ID，確保分散式環境下的唯一性 (與 Member Service、Auction Service 共用配置策略)
  - 資料庫設計: Bids 表的 bidId 欄位型態為 BIGINT (PostgreSQL) 或 long (C# Entity)
  - Redis 儲存: Sorted Set member 格式調整為 `{snowflakeBidId}:{timestamp}:{bidderId}`，利用 Snowflake ID 的時間排序特性
  - 優點: 
    - 系統一致性: 與 Member Service (userId) 和 Auction Service (auctionId) 使用相同 ID 策略
    - 時間排序: ID 本身包含時間戳，天然支援按 ID 排序即為時間排序
    - 分散式友善: 無需中央協調，適合 Redis + PostgreSQL 雙寫架構
    - 空間效率: 8 bytes vs GUID 的 16 bytes，節省 50% 儲存空間與索引大小
  - 注意事項: 需在應用程式啟動時初始化 Snowflake ID 生成器，確保 Worker ID 唯一性

### Session 2025-12-03

- Q: 服務水平擴展策略? → A: 選項 C - 單一實例搭配 Redis/PostgreSQL 擴展
  - Bidding Service 採用單一實例部署策略
  - 雪花 ID 生成器配置固定的 Worker ID (例如: Worker ID = 1, Datacenter ID = 1)
  - Redis 與 PostgreSQL 可獨立進行垂直或水平擴展以應對負載增長
  - 理由: 根據假設 A-003 (單一商品出價頻率不超過 1000 次/秒),單實例足以應對,架構更簡單
  - 服務本身設計為無狀態 (狀態存於 Redis/PostgreSQL),理論上可支援未來水平擴展
  - 未來擴展路徑: 若需要多實例,需實作 Worker ID 動態分配機制 (例如透過配置中心或資料庫分配)

- Q: 商品結束後的得標資訊更新機制? → A: 選項 C - Auction Service 主動查詢 Bidding Service
  - Bidding Service 職責: 僅提供 `GET /api/auctions/{id}/highest-bid` API 供查詢得標者
  - Auction Service 職責: 商品競標時間結束時,主動呼叫 Bidding Service API 查詢得標者資訊
  - Auction Service 取得得標資訊後,自行更新商品資料表 (winnerId, finalPrice 等)
  - 理由: 商品生命週期由 Auction Service 掌控,Bidding Service 專注競標邏輯,服務邊界清晰
  - 解耦優勢: Bidding Service 不需知道商品何時結束,不需監聽事件或定期檢查
  - 與 Session 2025-11-06 Q7 保持一致: Bidding Service 僅提供查詢,不負責通知或狀態管理

- Q: 是否在 Bidding Service 儲存出價者的 Email? → A: 選項 B - 僅儲存 bidderId,需要時查 Member Service
  - Bids 資料表不儲存 bidderEmail 欄位,僅儲存 bidderId (雪花 ID)
  - 需要 Email 時 (例如查詢「我的出價記錄」),跨服務呼叫 Member Service 取得使用者資訊
  - 理由: Member Service 是使用者資訊的單一資料來源 (Single Source of Truth)
  - 優點: 避免資料重複,Email 變更時無需同步更新多處
  - 快取策略: 實作短期快取 (例如 5 分鐘) 減少跨服務呼叫,但不永久儲存
  - 資料一致性: 保證查詢到的 Email 永遠是最新的

- Q: 敏感資料加密策略? → A: 選項 B - HTTPS 傳輸加密 + PostgreSQL 欄位層級加密
  - 傳輸層: 所有 API 強制使用 HTTPS (TLS 1.2+)
  - PostgreSQL 加密: 僅加密敏感欄位 `amount` (出價金額) 和 `bidderId` (出價者 ID)
  - 實作方式: 使用 EF Core Value Converter 在應用層自動加解密,加密演算法 AES-256-GCM
  - 金鑰管理: 加密金鑰儲存在 Azure Key Vault 或環境變數,不寫入程式碼或版本控制
  - Redis 不加密: 假設部署在可信內網,資料生命週期短 (< 7 天),效能優先
  - 理由: 平衡安全性與效能,保護核心商業資料,不影響 Redis < 10ms 寫入目標
  - 風險接受: 資料庫備份洩漏時,攻擊者無法直接讀取出價金額和出價者身份
  - 索引限制: 加密欄位無法建立索引,但查詢主要依賴 auctionId 和 bidAt,影響有限

- Q: 跨服務請求追蹤機制? → A: 選項 B - Correlation ID + 結構化日誌
  - 每個 API 請求生成唯一的 Correlation ID (GUID 格式)
  - HTTP Header 傳遞: 使用 `X-Correlation-ID` Header 在服務間傳遞追蹤 ID
  - 請求來源: 若前端或上游服務已提供 Correlation ID,則沿用;否則由 Bidding Service 生成
  - 跨服務呼叫: 呼叫 Auction Service 或 Member Service 時,在 HTTP Header 加入 `X-Correlation-ID`
  - 結構化日誌: 所有日誌 (Info/Warning/Error) 都包含 CorrelationId 欄位 (JSON 格式)
  - 日誌範例: `{"timestamp":"2025-12-03T10:15:30Z","level":"Info","correlationId":"abc-123","message":"Bid created","bidId":456}`
  - 問題排查: 透過 Correlation ID 串聯 Bidding Service → Auction Service → Member Service 的完整請求鏈
  - 實作工具: 使用 ASP.NET Core Middleware 自動注入 Correlation ID,Serilog 自動記錄到日誌
  - 理由: 輕量級且高效,無需引入複雜的分散式追蹤系統 (如 OpenTelemetry),但保留未來升級路徑

- Q: Redis Lua Script 併發控制邏輯? → A: 選項 A - 完整原子操作 (檢查 + 更新 Sorted Set + 更新 Hash)
  - Lua Script 執行流程:
    1. 從 `auction:{auctionId}:highest_bid` Hash 取得當前最高出價金額
    2. 檢查新出價金額是否 > 當前最高出價 (首次出價則檢查是否 >= 起標價)
    3. 若檢查通過,執行以下原子操作:
       - ZADD `auction:{auctionId}:bids` {amount} `{bidId}:{timestamp}:{bidderId}` (加入 Sorted Set)
       - HSET `auction:{auctionId}:highest_bid` bidId {bidId} bidderId {bidderId} amount {amount} bidAt {timestamp} (更新最高出價)
       - SADD `pending_bids` {bidId} (標記為待同步到 PostgreSQL)
       - 設定 TTL (商品結束時間 + 7 天)
    4. 回傳操作結果: success (出價成功) 或 rejected (金額不足)
  - 理由: Lua Script 保證原子性,避免競態條件 (兩筆出價同時通過檢查但只有一筆應該成功)
  - 錯誤處理: Script 執行失敗時回傳明確錯誤碼,應用層可重試或降級到 PostgreSQL
  - 效能優勢: 單次 Redis 往返完成所有檢查與更新,減少網路開銷
  - 測試策略: 使用併發測試驗證 100 個請求同時出價時,只有金額最高的成功

- Q: Auction Service API 依賴契約? → A: 選項 B - 定義 3 個必要端點 + 快取策略
  - **端點 1: 取得商品基本資訊** (用於出價驗證)
    - API: `GET /api/auctions/{id}/basic`
    - 用途: 驗證商品存在、取得擁有者、檢查狀態、取得起標價
    - 回應: `{ auctionId, ownerId, status, title, startingPrice, endTime }`
    - 快取策略: Redis 快取 5 分鐘, Key: `auction:basic:{auctionId}`
    - 錯誤碼: 404 (商品不存在)
  - **端點 2: 批次查詢商品資訊** (用於使用者出價記錄)
    - API: `POST /api/auctions/batch`
    - 用途: 一次查詢多個商品的標題和狀態 (避免 N+1 查詢問題)
    - 請求: `{ "auctionIds": [long, long, ...] }`
    - 回應: `{ "items": [{ auctionId, title, status }, ...] }`
    - 快取策略: 個別商品快取 5 分鐘,批次查詢先檢查快取,僅查詢未快取的商品
  - **端點 3: 驗證商品是否可出價** (可選,簡化出價流程)
    - API: `GET /api/auctions/{id}/can-bid?bidderId={bidderId}`
    - 用途: 一次呼叫檢查所有出價前提條件 (商品存在、未結束、非擁有者)
    - 回應: `{ "canBid": boolean, "reason": string | null }`
    - 快取策略: 不快取 (狀態可能快速變化)
  - **超時與降級策略**:
    - 呼叫超時: 100ms
    - 重試: 失敗時重試 1 次,總超時 200ms
    - 降級: 超時或失敗時使用快取過期資料,標記為 "資訊可能過時",記錄 Warning 日誌
  - **理由**: 明確 API 契約避免實作時反覆協調,批次查詢確保效能達標,快取策略減少跨服務呼叫開銷

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

### FR-000: Snowflake ID 生成
- **FR-000-1**: 系統必須使用雪花ID (Snowflake ID, 64-bit Long) 作為 Bid 的主鍵
- **FR-000-2**: 系統必須使用成熟的 .NET 雪花ID套件 (如 IdGen 或 Snowflake.Core) 生成 bidId
- **FR-000-3**: 雪花ID生成器必須配置 Worker ID 與 Datacenter ID 以確保分散式環境下的唯一性 (與 Member Service、Auction Service 共用配置策略)
- **FR-000-4**: bidId、auctionId、bidderId 必須統一使用 BIGINT (PostgreSQL) 或 long (C#) 型態

### FR-001: 出價提交 API
- **端點**: `POST /api/bids`
- **驗證**: 需要 JWT 認證
- **請求體**:
  ```json
  {
    "auctionId": "long (Snowflake ID)",
    "amount": "decimal"
  }
  ```
- **回應**:
  - 201: 出價成功
    ```json
    {
      "bidId": "long (Snowflake ID)",
      "auctionId": "long (Snowflake ID)",
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
- **併發定義**: 正常負載為 1000 requests/sec (單一商品), 高併發定義為 >1500 requests/sec (150% baseline)
- **監控告警**: Redis 連線使用率 > 80% 觸發告警
- 使用 Redis Lua Script 確保出價的原子性與併發安全
- **Lua Script 執行流程**:
  1. 從 `auction:{auctionId}:highest_bid` Hash 取得當前最高出價金額
  2. 檢查新出價金額是否 > 當前最高出價 (首次出價則檢查是否 >= 起標價)
  3. 若檢查通過,執行以下原子操作:
     - ZADD `auction:{auctionId}:bids` {amount} `{bidId}:{timestamp}:{bidderId}` (加入 Sorted Set)
     - HSET `auction:{auctionId}:highest_bid` (更新最高出價: bidId, bidderId, amount, bidAt)
     - SADD `pending_bids` {bidId} (標記為待同步到 PostgreSQL)
     - 設定 TTL (商品結束時間 + 7 天)
  4. 回傳操作結果: success (出價成功) 或 rejected (金額不足)
- 出價資料結構: Sorted Set (key: `auction:{auctionId}:bids`, score: amount, member: `{bidId}:{timestamp}:{bidderId}`)
- 最高出價: `auction:{auctionId}:highest_bid` (Hash 類型,包含 bidId, amount, bidderId, bidAt)
- 併發場景: 多筆出價同時進來時,Lua Script 原子性保證只有金額最高的成為最高出價
- 背景任務: 每秒執行一次,批次讀取 `pending_bids` Set 中的出價並寫入 PostgreSQL
- 寫入成功後從 `pending_bids` Set 移除該 bidId
- 降級機制: Redis 不可用時,切換為直接寫入 PostgreSQL (使用樂觀鎖作為後備)

### FR-005: 出價歷史紀錄
- 所有出價先寫入 Redis Sorted Set,立即可查詢
- 背景 Worker 非同步批次寫入 PostgreSQL 永久保存
- PostgreSQL 儲存欄位: bidId, auctionId, bidderId, amount, bidAt, createdAt
- 索引: auctionId + bidAt (降序), bidderId + bidAt (降序)
- Redis TTL: 商品結束時間 + 7 天 (之後僅從 DB 查詢)
- 寫入失敗重試: 指數退避 (1s, 2s, 4s),最多 3 次
- 死信佇列: 重試失敗後移入 `dead_letter_bids` Set,記錄 bidId、錯誤原因、時間戳

### FR-006: 查詢出價歷史 API
- **端點**: `GET /api/auctions/{auctionId}/bids`
- **查詢參數**: `page`, `pageSize` (預設 20)
- **查詢邏輯**:
  1. 先查詢 Redis Sorted Set: `auction:{auctionId}:bids`
  2. 如果 Redis 有資料,直接回傳 (尚未完全同步)
  3. 如果 Redis 無資料,查詢 PostgreSQL (已同步完成)
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
- **查詢邏輯**: 統一從 PostgreSQL 查詢 (WHERE bidderId = currentUserId)
- **跨服務查詢**: 
  - 呼叫 Auction Service 取得商品資訊 (標題、狀態)
  - 呼叫 Member Service 取得出價者 Email (不儲存在 Bids 表)
  - 實作短期快取 (5 分鐘) 減少跨服務呼叫
- **延遲說明**: 最新出價可能有最多 1 秒同步延遲 (背景任務週期)
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
  - 商品基本資訊 (擁有者、狀態、起標價、結束時間) - 用於出價驗證
  - 商品標題與狀態 (批次查詢) - 用於使用者出價記錄
  - 商品是否可出價 (可選) - 簡化出價流程
- 實作快取機制減少跨服務呼叫

### FR-008-1: Auction Service API 依賴契約

Bidding Service 需要 Auction Service 提供以下 API 端點:

#### 1. 取得商品基本資訊 (用於出價驗證)
- **端點**: `GET /api/auctions/{id}/basic`
- **用途**: 驗證商品存在、取得擁有者、檢查狀態、取得起標價
- **回應**:
  ```json
  {
    "auctionId": "long",
    "ownerId": "long",
    "status": "Active" | "Ended" | "Sold",
    "title": "string",
    "startingPrice": "decimal",
    "endTime": "ISO8601 datetime"
  }
  ```
- **錯誤碼**: 404 (商品不存在)
- **快取策略**: Redis 快取 5 分鐘, Key: `auction:basic:{auctionId}`
- **超時**: 100ms

#### 2. 批次查詢商品資訊 (用於使用者出價記錄)
- **端點**: `POST /api/auctions/batch`
- **方法**: AuctionServiceClient.GetAuctionsBatchAsync
- **用途**: 一次查詢多個商品的標題和狀態,避免 N+1 查詢問題
- **請求**:
  ```json
  {
    "auctionIds": ["long", "long", ...]
  }
  ```
- **回應**:
  ```json
  {
    "items": [
      {
        "auctionId": "long",
        "title": "string",
        "status": "Active" | "Ended" | "Sold"
      }
    ]
  }
  ```
- **快取策略**: 個別商品快取 5 分鐘,批次查詢先檢查快取,僅查詢未快取的商品
- **超時**: 100ms

#### 3. 驗證商品是否可出價 (可選)
- **端點**: `GET /api/auctions/{id}/can-bid?bidderId={bidderId}`
- **用途**: 一次呼叫檢查所有出價前提條件 (商品存在、未結束、非擁有者)
- **回應**:
  ```json
  {
    "canBid": true,
    "reason": null  // 或 "AuctionEnded" | "AuctionNotFound" | "IsOwner"
  }
  ```
- **快取策略**: 不快取 (狀態可能快速變化)
- **超時**: 100ms

#### 跨服務呼叫策略
- **超時設定**: 100ms
- **重試策略**: 失敗時重試 1 次,總超時 200ms
- **降級策略**: 超時或失敗時使用快取過期資料,標記為 "資訊可能過時",記錄 Warning 日誌
- **熔斷機制**: 連續失敗超過 5 次,暫停呼叫 30 秒,直接使用快取
- **Header 傳遞**: 所有跨服務呼叫必須傳遞 `X-Correlation-ID` 用於追蹤

### FR-009: 最高出價快取
- 使用 Redis Hash 儲存每個商品的最高出價
- Key: `auction:{auctionId}:highest_bid`
- Fields: bidId, bidderId, amount, bidAt
- 出價成功時,使用 Redis Lua Script 原子性檢查並更新最高出價
- TTL: 商品結束時間 + 1 天
- 查詢優先順序: Redis Hash → Redis Sorted Set ZREVRANGE → PostgreSQL

### FR-010: 查詢最高出價 API
- **端點**: `GET /api/auctions/{auctionId}/highest-bid`
- **查詢邏輯**:
  1. 先查詢 Redis Hash: `auction:{auctionId}:highest_bid`
  2. 如果 Redis 有資料,直接回傳
  3. 如果 Redis 無資料,查詢 PostgreSQL (SELECT * FROM Bids WHERE auctionId = ? ORDER BY amount DESC LIMIT 1)
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
**Bids 資料表** (PostgreSQL):
- bidId (BIGINT, PK) - 雪花ID (64-bit Long)
- auctionId (BIGINT, indexed) - 雪花ID，指向 Auction Service 的商品
- bidderId (BIGINT, indexed) - 雪花ID，指向 Member Service 的使用者 (不儲存 Email,需要時查 Member Service)
- amount (decimal, encrypted) - 使用 AES-256-GCM 加密
- bidAt (timestamp, indexed)
- createdAt (timestamp) - 實際寫入 DB 的時間
- SyncedFromRedis (boolean) - 標記是否從 Redis 同步而來 (PascalCase 符合 C# 命名慣例)

**Redis 資料結構**:
1. Sorted Set: `auction:{auctionId}:bids`
   - Score: amount (decimal as string)
   - Member: `{snowflakeBidId}:{timestamp}:{bidderId}` (所有 ID 皆為雪花ID)
   
2. Hash: `auction:{auctionId}:highest_bid`
   - Fields: bidId, bidderId, amount, bidAt
   
3. Set: `pending_bids` 
   - Members: bidId (待寫入 DB 的出價 ID)

4. Set: `dead_letter_bids`
   - Members: bidId (重試失敗的出價 ID)
   
5. Hash: `dead_letter_bid:{bidId}`
   - Fields: bidId, error, timestamp, retryCount

**PostgreSQL 索引**:
- (auctionId, bidAt DESC): 出價歷史查詢
- (bidderId, bidAt DESC): 使用者出價記錄查詢
- (auctionId, amount DESC): 最高出價備援查詢

### FR-014: 錯誤處理
- 400 Bad Request: 出價金額不足、驗證失敗
- 401 Unauthorized: Token 無效或過期
- 403 Forbidden: 對自己商品出價
- 404 Not Found: 商品不存在
- 409 Conflict: 商品已結束、併發衝突
- 500 Internal Server Error: 系統異常
- 503 Service Unavailable: Redis 與 PostgreSQL 均不可用

### FR-014-1: Redis 降級機制
- **健康檢查**: 獨立 HostedService 每 10 秒執行 `PING` 命令
- **降級觸發**: 連續 3 次失敗 → 設定全域標記 `UsePostgreSQLFallback = true` (in-memory flag, not persisted) (in-memory flag, not persisted)
- **降級模式**: 出價 API 檢查標記,為 true 時直接寫 PostgreSQL (使用 EF Core 樂觀鎖)
- **自動恢復**: 連續 5 次健康檢查成功 → 設定 `UsePostgreSQLFallback = false`
- **服務重啟**: 預設為 Redis-first mode (UsePostgreSQLFallback = false), 健康檢查重新評估
- **冷卻期**: 狀態切換後 30 秒內不再切換,防止抖動
- **告警**: 降級/恢復事件透過 ILogger 記錄 Warning 級別,並發送通知 (Email/Slack)
- **監控指標**: 記錄當前模式 (Redis/PostgreSQL)、切換次數、降級持續時間

### FR-015: 日誌與監控
- 記錄所有出價嘗試 (成功與失敗)
- 記錄併發衝突次數
- 記錄跨服務呼叫延遲與 Correlation ID
- 記錄快取命中率
- 記錄 Redis 降級/恢復事件 (Warning 級別)
- 記錄背景 Worker 同步延遲 (正常 < 1 秒)
- 記錄死信佇列大小與新增事件 (Error 級別)
- 使用結構化日誌 (JSON 格式),所有日誌包含 `correlationId` 欄位
- Correlation ID 追蹤: 透過 `X-Correlation-ID` Header 在服務間傳遞,串聯完整請求鏈
- 提供 Prometheus metrics: bid_requests_total, bid_latency_seconds, redis_fallback_active, dead_letter_queue_size

### FR-016: 效能優化
- 出價寫入: 優先寫 Redis (< 10ms),非同步批次寫 PostgreSQL
- 最高出價查詢: 優先 Redis Hash,無資料才查 PostgreSQL
- 出價歷史查詢: 優先 Redis Sorted Set,無資料才查 PostgreSQL
- 使用者出價記錄: 統一從 PostgreSQL 查詢 (利用 bidderId 索引)
- 跨服務呼叫實作快取與超時控制 (timeout: 100ms)
- Redis 連線池配置: min 10, max 100
- PostgreSQL 連線池配置: min 10, max 50
- 背景 Worker 批次大小: 每次最多處理 1000 筆出價
- Redis 資料生命週期: 出價寫入 → 同步 DB → 清除或標記 processed → 商品結束後保留 7 天 → 過期刪除

### FR-017: 安全性
- 所有 API 需要 JWT 驗證
- 強制使用 HTTPS (TLS 1.2+) 傳輸加密
- PostgreSQL 欄位層級加密: amount 和 bidderId 使用 AES-256-GCM 加密
- 加密金鑰儲存在 Azure Key Vault 或安全的環境變數,不寫入程式碼
- 驗證使用者身份與 Token 中的 UserId 一致
- 防止 SQL Injection (使用參數化查詢)
- 敏感資訊 (如出價者身份、出價金額) 不以明文記錄於日誌

### FR-018: 可測試性
- 所有業務邏輯封裝於 Service 層
- 資料存取使用 Repository 模式
- 跨服務呼叫使用介面抽象
- 提供 In-Memory 測試替代實作

---

### 關鍵實體

- **Bid (出價)**: 代表一筆競標出價，包含 bidId (雪花ID, 64-bit Long, 主鍵)、auctionId (雪花ID, 外鍵指向 Auction Service)、bidderId (雪花ID, 外鍵指向 Member Service)、金額、出價時間等資訊，與 Auction 為多對一關係
- **Auction (商品)**: 外部實體，由 Auction Service 管理，Bidding Service 透過跨服務呼叫取得商品資訊，auctionId 為雪花ID
- **User (使用者)**: 外部實體，由 Member Service 管理，出價者身份透過 JWT 驗證，userId 為雪花ID

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
- [ ] 通過 1000 併發使用者同時出價的壓力測試
- [ ] 通過 5000 併發使用者同時查詢最高出價的壓力測試
- [ ] 通過混合讀寫場景的壓力測試（500 出價 + 2000 查詢）
- [ ] 通過多商品併發測試（10 個商品，各 100 個出價）
- [ ] 無重複得標者 (同一商品只有一個最高出價)
- [ ] 併發衝突正確回傳 409 錯誤
- [ ] 高併發下錯誤處理穩定，系統無崩潰

### SC-004: 資料一致性
- [ ] 所有出價永久保存於 PostgreSQL,無遺失
- [ ] Redis 與 PostgreSQL 最終一致性 (背景任務同步延遲 < 1 秒)
- [ ] Redis AOF 持久化確保重啟後資料可恢復
- [ ] 背景 Worker 失敗重試機制正確運作
- [ ] 跨服務資料同步正確

### SC-005: 測試覆蓋率
- [ ] 單元測試覆蓋率 > 80%
- [ ] 整合測試覆蓋所有 API 端點
- [ ] 包含併發場景測試
- [ ] 負載測試（Load Testing）覆蓋所有核心 API（使用 NBomber）
  - 併發出價測試（1000 requests）
  - 最高出價查詢測試（5000 concurrent reads）
  - 出價歷史查詢測試（1000 concurrent paginated reads）
  - 混合讀寫場景測試（500 bids + 2000 queries）
  - 錯誤處理壓測（invalid bids under high concurrency）
  - 多商品併發測試（10 auctions, 100 bids each）
- [ ] 負載測試（Load Testing）覆蓋所有核心 API（使用 NBomber）
  - 併發出價測試（1000 requests）
  - 最高出價查詢測試（5000 concurrent reads）
  - 出價歷史查詢測試（1000 concurrent paginated reads）
  - 混合讀寫場景測試（500 bids + 2000 queries）
  - 錯誤處理壓測（invalid bids under high concurrency）
  - 多商品併發測試（10 auctions, 100 bids each）

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
- **語言/框架**: ASP.NET Core 10
- **部署策略**: 單一實例部署 (雪花 ID 使用固定 Worker ID),服務無狀態設計保留未來水平擴展能力
- **主要儲存**: Redis (寫入層) + PostgreSQL (持久層)
- **快取策略**: Write-Behind Cache (先寫 Redis,非同步回寫 DB)
- **併發控制**: Redis 原子操作 (ZADD, Lua Script) + PostgreSQL 降級時使用樂觀鎖
- **背景任務**: IHostedService 實作批次同步 Worker
- **跨服務通訊**: HTTP REST API (使用 HttpClient)
- **Redis 持久化**: AOF (appendfsync everysec)

### 依賴服務
- **Auction Service**: 查詢商品資訊、狀態、擁有者
- **Member Service**: 驗證 JWT Token (間接依賴)
- **未來依賴**: Notification Service (由 Auction Service 呼叫,通知得標者)

### 限制
- 不支援出價撤回
- 不支援代理出價 (Proxy Bidding)
- 不支援即時推送通知 (WebSocket/SignalR),採用前端輪詢機制
- 不負責得標者通知 (由 Auction Service 協調,未來由 Notification Service 發送)
- 歷史查詢僅支援時間排序,不支援金額排序

---

## 風險與假設

### 風險
- **R-001**: Redis 故障導致出價服務完全不可用 → 降級到直接寫 PostgreSQL,犧牲效能保證可用性
- **R-002**: 背景 Worker 故障導致 Redis 資料未同步到 DB → 實作健康檢查,監控同步延遲,背景 Worker 自動重啟
- **R-003**: 高併發 (定義見 FR-004: >1500 requests/sec, 150% baseline) 導致 Redis 連線耗盡 → 連線池配置 max 100,監控連線使用率,超過 80% 使用率觸發告警
- **R-004**: Redis 與 DB 資料不一致 → AOF 持久化 + 對帳機制 (定期比對 Redis 與 DB)
- **R-005**: Auction Service 不可用 → 快取商品資訊,設定合理超時 (100ms)

### 假設
- **A-000**: Bid ID 使用雪花ID生成，與 Member Service 的 User ID 及 Auction Service 的 Auction ID 使用相同策略，確保系統一致性
- **A-000-1**: 雪花ID生成使用成熟的 .NET 套件 (IdGen 或 Snowflake.Core)，需配置 Worker ID 與 Datacenter ID
- **A-001**: Auction Service 提供查詢商品資訊的 API
- **A-002**: Redis 可用性 > 99.9%,啟用 AOF 持久化
- **A-003**: 出價頻率不超過 1000 次/秒 (單一商品)
- **A-004**: 背景 Worker 每秒同步延遲可接受 (最終一致性)
- **A-005**: PostgreSQL 能承受每秒 1000 筆批次寫入

---

## 待解決問題
- ~~Q-001: 是否需要實作出價通知功能 (WebSocket/SignalR)?~~ → 已澄清 (Q6): 使用前端輪詢
- ~~Q-002: 商品結束後,如何通知得標者? (跨服務或事件驅動?)~~ → 已澄清 (Q7): Auction Service 負責協調
- Q-003: 是否需要實作出價上限 (max bid per user)? → 範圍外,未來需求時再評估

---

**憲法檢查**:
- [x] **原則 I: 程式碼品質優先** - 規格要求測試覆蓋率 > 80%、Repository 模式、介面抽象
- [x] **原則 II: 測試驅動開發** - SC-005 明確要求單元測試與整合測試
- [x] **原則 III: 使用者體驗一致性** - API 回應格式統一、錯誤訊息清楚
- [x] **原則 IV: 效能要求** - 明確定義回應時間目標與快取策略
- [x] **原則 V: 可觀測性** - FR-015 要求結構化日誌、APM 追蹤、Health Check
- [x] **原則 VI: 簡潔性與 YAGNI** - 不實作未明確要求的功能 (如代理出價),專注核心競標邏輯

