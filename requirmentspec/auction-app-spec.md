# 拍賣程式規格檔

## 1. 簡介

本拍賣程式提供一個線上拍賣平台，讓使用者可輕鬆瀏覽、競標與管理商品，支援手機與網頁版，並強調良好體驗。系統採用模組化單體架構，整合競標、訂單與金流服務。

## 2. 功能需求

### 2.1 首頁

- 顯示所有拍賣商品清單，支援分類篩選與關鍵字搜尋。
- 商品卡片顯示名稱、目前出價、結束時間等資訊。
- 點擊商品卡片可進入詳細頁。
- 商品卡片提供 icon 按鈕，能將商品加入/移除追蹤清單。

### 2.2 商品詳細頁

- 顯示商品描述、目前出價、結束時間等詳細資訊。
- 提供出價功能，使用者可輸入金額進行出價。
- 顯示所有出價歷史紀錄（可選）。

### 2.3 追蹤清單頁

- 顯示使用者追蹤的商品清單。
- 支援取消追蹤功能。

### 2.4 我的出價頁

- 顯示使用者曾出價過的商品清單。
- 顯示每個商品的出價狀態（如領先、被超越）。

### 2.5 出價紀錄頁

- 顯示所有出價紀錄（含過往與目前結果）。
- 支援依時間篩選紀錄。

### 2.6 帳號設定頁

- 提供個人資料編輯（名稱、電子郵件）。
- 支援更改密碼與登出。

### 2.7 通知中心

- 在網站右上角應有通知 icon，點擊後可透過 API 讀取歷史通知，例如「您追蹤的商品已上架」、「恭喜您得標」等。
- 未讀通知應有紅點提示。

### 2.8 訂單管理頁 (New)

- **買家視角**:
    - 查看「我的訂單」列表，包含狀態 (待付款、待出貨、已完成)。
    - 對於「待付款」訂單，提供「前往結帳」按鈕。
- **賣家視角**:
    - 查看「銷售訂單」列表。
    - 對於「已付款」訂單，提供「更新出貨狀態」功能。

### 2.9 結帳與支付 (New)

- 使用者點擊「前往結帳」後，系統導向第三方支付頁面 (如 Stripe/ECPay)。
- 支付成功後，自動跳轉回訂單完成頁面，並顯示交易成功訊息。

## 3. 系統架構

### 3.1 NX 專案結構

- 採用 NX 框架管理多專案架構，利於共用與獨立功能模組化。
- 專案分為：
  - 前端 Web（Next.js）
  - 行動版（Next.js）
  - 共用函式庫（libs/）：統一管理 API 定義、UI 元件、i18n 等。

### 3.2 前端

- 使用 Next.js（基於 React）實現 SPA 與多頁面路由。
- 路由規劃：/auctions、/mybids、/orders (訂單)、/account、/follow 等。
- 樣式採 SCSS Toolkit，統一設計與排版。
- 支援 RWD 響應式設計與無障礙（a11y）。
- 國際化（i18n）：支援多語系切換，預設 zh-TW。

### 3.3 後端
  
- **單一專案架構 (Modular Monolith)**: 採用 ASP.NET Core 10 (C#) 開發單一的 Web API 應用程式。
- **核心模組**:
    - **Member**: 會員管理。
    - **Auction**: 商品與競標活動。
    - **Bidding**: 出價處理。
    - **Ordering**: 訂單生命週期管理。
    - **Payment**: 金流交易處理。
    - **Notification**: 訊息推播。
- **API 設計**: RESTful 風格，使用 Swagger/OpenAPI 產生文件。
- **安全性**: 使用 JWT 進行身份驗證與授權。

### 3.4 資料庫

- 使用 PostgreSQL 作為主要資料庫，利用 **Schemas** (`member`, `auction`, `ordering`, `payment`...) 隔離不同模組的資料表。

## 4. 使用者角色

- **一般使用者**：可瀏覽、追蹤、出價商品、下單付款與管理個人帳號。
- **管理員**：可管理商品資料與使用者帳號，具備後台管理權限。

## 5. 技術規格

### 5.1 前端

- 程式語言：TypeScript/JavaScript
- 框架：Next.js
- 樣式：Ant Design, SCSS
- 狀態管理：Redux Toolkit
- API 通訊：Fetch / Axios
- 程式碼品質：ESLint、Prettier
- 測試：Jest（單元）、Playwright（E2E）

### 5.2 後端
  
- 程式語言：C#
- 框架：ASP.NET Core 10
- 資料庫：PostgreSQL
- ORM：Entity Framework Core
- 測試：xUnit

### 5.3 其他

- 容器化：Docker
- CI/CD：GitHub Actions
- 部署：
  - 前端：Vercel / Netlify
  - 後端：Azure App Service / AWS Elastic Beanstalk / Docker Host
- 版本管理：Git，採 Git flow 分支策略

## 6. API 規格

### 6.1 取得商品清單

- `GET /api/auctions`
  - 參數：`category`（選填）、`search`（選填）
  - 回應：200 OK（商品清單），400 Bad Request

### 6.2 新增追蹤商品

- `POST /api/auctions/{id}/follow`
  - 參數：`id` (Path Parameter)
  - 回應：201 Created，400 Bad Request

### 6.3 取得商品詳細

- `GET /api/auctions/{id}`
  - 回應：200 OK（商品詳細），404 Not Found

### 6.4 出價

- `POST /api/bids`
  - Body：`{ "auctionId": "...", "amount": 1000 }`
  - 回應：201 Created，400 Bad Request，403 Forbidden

### 6.5 取得我的出價紀錄

- `GET /api/me/bids`
  - 回應：200 OK（出價紀錄）

### 6.6 取得追蹤清單

- `GET /api/me/follows`
  - 回應：200 OK（追蹤商品清單）

### 6.7 帳號管理

- `GET /api/members/me`、`PUT /api/members/me`
  - 回應：200 OK，400 Bad Request

### 6.8 取得通知紀錄

- `GET /api/notifications/history`
  - 回應：200 OK（該使用者的通知歷史紀錄），401 Unauthorized

### 6.9 訂單管理 (New)

- `GET /api/orders`: 取得我的訂單列表 (支援 filter: role=buyer|seller)。
- `GET /api/orders/{id}`: 取得單筆訂單詳情。

### 6.10 支付 (New)

- `POST /api/payments/checkout`: 針對訂單建立付款 Session。
  - Body: `{ "orderId": "..." }`
  - Response: `{ "checkoutUrl": "https://stripe.com/..." }`

## 7. 測試計畫

- **單元測試**：涵蓋各函式、元件與模組，Jest（前端）、xUnit（後端）。
- **整合測試**：驗證 API 端點與資料庫互動，特別是 **結標 -> 訂單 -> 支付** 的流程。
- **E2E 測試**：Playwright，模擬實際情境。
- 覆蓋率目標：單元測試 80%。

## 8. 部署計畫

- 前端：Vercel 或 Netlify
- 後端：Azure App Service 或單一 Docker 容器
- CI/CD：GitHub Actions，自動化建構與測試

## 9. 安全性與效能

- 防範 XSS/CSRF，API rate limit。
- 前端效能優化：圖片壓縮、Lazy load。
- 後端效能：使用 Async/Await，資料庫索引優化，Redis 快取（可選）。

## 10. 日誌與監控

- 統一輸出日誌 (Structured Logging)。
- 使用 Application Insights 或類似工具監控應用程式健康狀態。

## 11. 參考文件

- [ASP.NET Core](https://dotnet.microsoft.com/apps/aspnet)
- [PostgreSQL](https://www.postgresql.org/docs/)
- [Next.js](https://nextjs.org/)
