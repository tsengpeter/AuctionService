# 後端系統設計規格書 (Modular Monolith Architecture)

## 1. 總覽 (Overview)
本文件定義競標網站後端系統的架構。採用 **模組化單體 (Modular Monolith)** 設計，將會員、拍賣、競標、通知、訂單與支付等業務邏輯集中於單一專案中實作，透過統一的 API 介面提供服務。

## 2. 技術架構 (Technical Architecture)
- **架構模式**: 模組化單體 (Modular Monolith)
- **開發框架**: ASP.NET Core 10 (C#)
- **資料庫**: PostgreSQL (單一資料庫實例)
    - **邏輯隔離**: 使用 PostgreSQL **Schemas** (`member`, `auction`, `bidding`, `ordering`, `payment`, `notification`) 來區分模組邊界。
- **模組間通訊**: 
    - **同步**: 方法呼叫 (Method Calls)
    - **非同步**: 記憶體內事件 (In-Memory Events via MediatR)

## 3. 系統架構圖 (System Architecture Diagram)
```
[Clients: Web/Mobile App]
         | (HTTPS / RESTful API)
         v
[後端應用程式 (Backend Application)]
+-----------------------------------------------------------------------------------------------+
|  [API Router / Controllers (Unified Interface)]                                               |
|       |              |              |              |              |              |            |
|       v              v              v              v              v              v            |
| [會員模組]      [商品拍賣模組]    [競標模組]      [訂單模組]      [支付模組]      [通知模組]    |
| (Member)        (Auction)        (Bidding)     (Ordering)     (Payment)      (Notification)|
|       |              |              |              |              |              |            |
+-------+--------------+--------------+--------------+--------------+--------------+------------+
        |              |              |              |              |              |
        v              v              v              v              v              v
[單一資料庫 (PostgreSQL)]
-------------------------------------------------------------------------------------
| Schema: member | auction | bidding | ordering | payment | notification |
-------------------------------------------------------------------------------------
```

## 4. 資料庫詳細設計 (Database Schema Design)

### 4.1 Schema: `member` (會員模組)
*負責使用者身份與基本資料*

- **Table `users`**: `id` (PK), `username`, `email`, `password_hash`, `role`, ...
- **Table `refresh_tokens`**: `id` (PK), `user_id`, `token`, `expires_at`

### 4.2 Schema: `auction` (商品拍賣模組)
*負責商品資訊與拍賣狀態*

- **Table `categories`**: `id`, `name`, `parent_id`
- **Table `auctions`**: 
    - `id` (PK, UUID)
    - `owner_id` (賣家)
    - `status` (0:Draft, 1:Active, 2:Ended, 3:SoldOut_PendingValidation)
    - `winner_id` (得標者, Nullable)
    - `sold_amount` (成交價)
    - `end_time`
    - ...
- **Table `auction_images`**: `id`, `auction_id`, `url`

### 4.3 Schema: `bidding` (競標模組)
*負責出價紀錄*

- **Table `bids`**: `id` (PK), `auction_id`, `bidder_id`, `amount`, `bid_time`

---

### 4.4 Schema: `ordering` (訂單模組) **[NEW]**
*負責得標後的訂單履約與狀態追蹤*

#### Table: `orders`
| Column Name | Data Type | Constraints | Description |
|---|---|---|---|
| `id` | `uuid` | **PK** | 訂單 ID |
| `auction_id` | `uuid` | Unique, Not Null | 關聯商品 (一對一) |
| `buyer_id` | `uuid` | Not Null | 買家 ID (得標者) |
| `seller_id` | `uuid` | Not Null | 賣家 ID |
| `amount` | `decimal` | Not Null | 訂單總金額 (成交價) |
| `status` | `varchar(20)` | Not Null | PendingPayment, Paid, Shipped, Completed, Cancelled |
| `shipping_address` | `jsonb` | Nullable | 收件地址 |
| `created_at` | `timestamp` | Default Now | 建立時間 (通常為結標時間) |
| `updated_at` | `timestamp` | | 最後更新時間 |

---

### 4.5 Schema: `payment` (支付模組)
*負責處理針對「訂單」的金流交易*

#### Table: `transactions`
| Column Name | Data Type | Constraints | Description |
|---|---|---|---|
| `id` | `uuid` | **PK** | 交易 ID |
| `order_id` | `uuid` | Not Null | **關聯訂單** (`ordering.orders`) |
| `user_id` | `uuid` | Not Null | 付款人 ID |
| `amount` | `decimal` | Not Null | 交易金額 |
| `provider` | `varchar(50)` | Not Null | Stripe, ECPay |
| `external_id` | `varchar(100)` | Nullable | 第三方交易序號 |
| `status` | `varchar(20)` | Not Null | Pending, Completed, Failed |

---

### 4.6 Schema: `notification` (通知模組)
*負責通知紀錄*

- **Table `notifications`**: `id`, `user_id`, `type`, `message`, `is_read`

## 5. 核心模組與 API 設計 (Core Modules & API Design)

### 5.1 訂單模組 (Ordering Module)
- **職責**: 
    - 監聽 `AuctionWon` 事件自動建立訂單。
    - 管理訂單狀態流轉 (付款 -> 出貨 -> 完成)。
- **API Endpoints**:
    - `GET /api/orders`: 查詢我的訂單 (買家/賣家視角)。
    - `GET /api/orders/{id}`: 訂單詳情。
    - `POST /api/orders/{id}/shipping`: 賣家更新出貨資訊。
    - `POST /api/orders/{id}/complete`: 買家確認收貨 (完成訂單)。

### 5.2 支付模組 (Payment Module)
- **職責**: 針對特定 `OrderId` 進行付款。
- **API Endpoints**:
    - `POST /api/payments/checkout`: 建立付款 Session (傳入 `orderId`)。
    - `POST /api/payments/webhook`: 金流回調。

## 6. 關鍵業務流程 (Business Processes)

### 6.1 從結標到付款完成 (Order Fulfillment Flow)

1.  **結標 (Auction Module)**: 
    - 拍賣時間結束，系統確認有出價。
    - `Auction` 狀態更新為 `SoldOut`。
    - 發布 `AuctionWonEvent` (包含 `AuctionId`, `WinnerId`, `Amount`)。

2.  **建立訂單 (Ordering Module)**:
    - 監聽 `AuctionWonEvent`。
    - 在 `ordering.orders` 建立一筆新訂單，狀態為 `PendingPayment`。
    - 通知買家：「恭喜得標！請前往結帳」。

3.  **買家付款 (Payment Module)**:
    - 買家在前端點擊「前往付款」，呼叫 `POST /api/payments/checkout` (帶入 `OrderId`)。
    - Payment Module 導向第三方金流頁面。
    - 買家完成刷卡。

4.  **付款確認 (Payment & Ordering Modules)**:
    - 第三方金流呼叫 Webhook。
    - **Payment Module** 確認交易成功，寫入 `transactions`，發布 `PaymentSucceededEvent`。
    - **Ordering Module** 監聽此事件，將訂單狀態更新為 `Paid`。
    - 通知賣家：「訂單已付款，請準備出貨」。

## 7. 測試策略
- **整合測試重點**: 模擬從 `Auction End` -> `Order Created` -> `Payment Success` -> `Order Paid` 的完整狀態流轉，確保事件驅動機制運作正常。
