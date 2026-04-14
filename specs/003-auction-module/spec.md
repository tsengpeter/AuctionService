# Feature Specification: Auction 模組

**Feature Branch**: `003-auction-module`  
**Created**: 2026-04-14  
**Status**: Draft  
**Input**: User description: "實作 Auction 模組：賣家建立拍賣商品（標題、描述、起標價、結標時間、分類、最多5張圖片 URL）、編輯草稿商品、上架商品（Draft→Active）、瀏覽商品列表（支援分類篩選、關鍵字搜尋、分頁 pageSize=20）、商品詳細頁（含目前最高出價）、將商品加入/移除追蹤清單、查詢追蹤清單、排程結標（結標時間到自動將 Active→Ended，若有出價則發布 AuctionWonEvent 含 AuctionId/WinnerId/SoldAmount）；商品狀態機：Draft→Active→Ended；schema: auction，tables: auctions, auction_images, categories, watchlist"

## User Scenarios & Testing *(mandatory)*

> **開發優先順序原則**：以競標者（買家）的使用旅程為主軸依序實作。競標者需要先能發現、瀏覽、追蹤商品，才有動機出價。賣家的商品管理功能（建立、編輯、上架）作為後半段實作，並以種入測試資料支撐前期的競標者功能測試。

### User Story 1 - 瀏覽商品列表與搜尋 (Priority: P1)

任何用戶（含未登入訪客）可以瀏覽所有 Active 狀態的拍賣商品，支援依分類篩選、關鍵字搜尋，以及分頁瀏覽（每頁 20 筆）。

**Why this priority**: 商品發現是競標旅程的起點。競標者必須能找到感興趣的商品才有後續行為，列表與搜尋功能直接決定平台的使用率與競標活躍度。可透過預先種入測試商品資料獨立驗證此功能。

**Independent Test**: 預先種入數筆 Active 商品後，測試無條件查詢、分類篩選、關鍵字搜尋三種情境均回傳正確結果及分頁資訊。

**Acceptance Scenarios**:

1. **Given** 系統中有多筆 Active 商品，**When** 用戶請求第一頁商品列表，**Then** 回傳最多 20 筆商品及當前頁碼、總筆數資訊。
2. **Given** 系統中有屬於特定分類的商品，**When** 用戶以分類 ID 篩選，**Then** 只回傳符合該分類的商品。
3. **Given** 商品標題含有特定關鍵字，**When** 用戶以該關鍵字搜尋，**Then** 回傳標題含有該關鍵字的商品。
4. **Given** 商品總數超過 20 筆，**When** 用戶請求第二頁，**Then** 回傳第 21-40 筆商品。
5. **Given** 篩選條件無符合結果，**When** 用戶發送請求，**Then** 回傳空陣列及 total=0，HTTP 200。
6. **Given** 系統存有 Draft 或 Ended 狀態的商品，**When** 用戶瀏覽列表，**Then** 這些商品不出現在結果中。

---

### User Story 2 - 查看商品詳情（含目前最高出價） (Priority: P2)

競標者可以查看特定商品的完整資訊，包括標題、描述、圖片、起標價、結標時間、狀態，以及目前最高出價金額，協助其決定是否出價。

**Why this priority**: 詳情頁是競標者決策的核心頁面。最高出價資訊讓競標者了解當前競爭情況，是出價行為前的必要資訊依據。

**Independent Test**: 查詢存在的商品 ID，驗證回傳資料包含完整欄位，且最高出價欄位正確（若無出價則顯示 null）。

**Acceptance Scenarios**:

1. **Given** 商品存在，**When** 用戶查詢商品詳情，**Then** 回傳完整商品資料，包含圖片列表與目前最高出價。
2. **Given** 商品不存在，**When** 用戶查詢，**Then** 回傳 404 Not Found。
3. **Given** 商品尚無出價，**When** 用戶查詢詳情，**Then** 最高出價欄位為 null。

---

### User Story 3 - 追蹤清單（加入/移除/查詢） (Priority: P3)

已登入的競標者可以將感興趣的商品加入個人追蹤清單，方便日後快速找到，也可以移除不再感興趣的商品，並隨時查詢自己的追蹤清單。

**Why this priority**: 追蹤清單是競標者持續參與平台的黏著力機制，讓買家不需重複搜尋即可回到感興趣的商品，提升競標轉化率。

**Independent Test**: 登入後加入商品至追蹤清單，查詢追蹤清單可見該商品；移除後再次查詢不再顯示。重複加入/移除操作均為 204 不報錯。

**Acceptance Scenarios**:

1. **Given** 已登入用戶，**When** 將商品加入追蹤清單，**Then** 操作成功（204），清單中出現該商品。
2. **Given** 商品已在追蹤清單中，**When** 再次加入，**Then** 冪等操作，回傳 204，不重複建立。
3. **Given** 商品在追蹤清單中，**When** 用戶移除，**Then** 操作成功（204），清單中移除該商品。
4. **Given** 商品不在追蹤清單中，**When** 用戶嘗試移除，**Then** 冪等操作，回傳 204，不報錯。
5. **Given** 追蹤清單中有商品，**When** 用戶查詢追蹤清單，**Then** 回傳含商品基本資訊的清單。
6. **Given** 未登入用戶，**When** 嘗試加入追蹤清單，**Then** 系統回傳未授權（401）。

---

### User Story 4 - 排程結標（競標者得知結果） (Priority: P4)

系統排程每分鐘自動掃描已到結標時間的 Active 商品，將其狀態變更為 Ended。若商品有出價紀錄，系統發布 AuctionWonEvent（含 AuctionId、WinnerId、SoldAmount、SellerId），觸發後續得標通知與訂單建立，讓競標者即時得知結果。

**Why this priority**: 結標是競標者旅程的終點，得標通知是完整競標體驗的最後一哩路。此功能在競標者功能齊備後即可獨立實作與驗證。

**Independent Test**: 預先植入一筆結標時間為過去的 Active 商品（含出價紀錄），觸發排程後驗證商品狀態變為 Ended，且 AuctionWonEvent 已發布含正確的 WinnerId 與 SoldAmount。

**Acceptance Scenarios**:

1. **Given** Active 商品的結標時間已到，**When** 排程執行，**Then** 商品狀態變更為 Ended。
2. **Given** 商品結標且有出價紀錄，**When** 排程執行，**Then** 系統發布 AuctionWonEvent，含最高出價者 ID、成交金額與賣家 ID。
3. **Given** 商品結標但無任何出價，**When** 排程執行，**Then** 商品狀態變為 Ended，不發布 AuctionWonEvent。
4. **Given** 同一商品已處於 Ended 狀態，**When** 排程再次執行，**Then** 不重複更新（冪等）。
5. **Given** Active 商品的結標時間尚未到，**When** 排程執行，**Then** 商品狀態不變。

---

### User Story 5 - 建立拍賣商品（賣家） (Priority: P5)

已登入的賣家可以建立一個新的拍賣商品（填寫標題、描述、起標價、結標時間、分類，以及最多 5 張圖片 URL），初始狀態為 Draft，為後續上架做準備。

**Why this priority**: 賣家建立商品功能是平台商品供給端的必要功能，在競標者端功能完備後實作，可同時驗證端對端流程。

**Independent Test**: 賣家登入後建立商品，驗證回傳 201 含新商品 ID，DB 中存在一筆 Draft 狀態的商品記錄。

**Acceptance Scenarios**:

1. **Given** 賣家已登入，**When** 提交有效的商品資料（標題、起標價 > 0、結標時間 > 現在），**Then** 系統建立一筆 Draft 狀態的商品，並回傳新商品 ID（201）。
2. **Given** 賣家未登入，**When** 嘗試建立商品，**Then** 系統回傳未授權（401）。
3. **Given** 起標價為 0 或負數，**When** 提交建立請求，**Then** 系統回傳驗證錯誤（422）。
4. **Given** 結標時間早於現在加 1 分鐘，**When** 提交建立請求，**Then** 系統回傳驗證錯誤（422）。
5. **Given** 圖片 URL 超過 5 張，**When** 提交建立請求，**Then** 系統回傳驗證錯誤（422）。

---

### User Story 6 - 編輯草稿商品（賣家） (Priority: P6)

賣家可以修改自己建立的 Draft 狀態商品的任何欄位（標題、描述、起標價、結標時間、分類、圖片），但一旦上架（Active）或結標（Ended）則不得再修改。

**Why this priority**: 編輯功能讓賣家在上架前修正錯誤，確保商品資訊品質，是賣家端的配套功能。

**Independent Test**: 建立一筆 Draft 商品後進行更新，驗證欄位正確變更；再將商品上架後嘗試更新，驗證系統回傳 409。

**Acceptance Scenarios**:

1. **Given** 商品處於 Draft 狀態且賣家為擁有者，**When** 提交有效的更新資料，**Then** 商品欄位更新成功，回傳 200。
2. **Given** 商品不處於 Draft 狀態，**When** 賣家嘗試更新，**Then** 系統回傳衝突錯誤（409）。
3. **Given** 商品屬於其他賣家，**When** 當前用戶嘗試更新，**Then** 系統回傳禁止操作（403）。
4. **Given** 更新資料違反驗證規則（如起標價 ≤ 0），**When** 提交請求，**Then** 系統回傳驗證錯誤（422）。

---

### User Story 7 - 上架商品（賣家） (Priority: P7)

賣家確認商品內容後，可將 Draft 狀態商品上架（變更為 Active），使商品對所有用戶可見並開放競標。

**Why this priority**: 上架功能串連賣家建立商品與競標者發現商品的完整鏈路，是最後補足的賣家端操作。

**Independent Test**: 對 Draft 商品發送上架請求，驗證商品狀態變為 Active，且商品出現在公開列表中。

**Acceptance Scenarios**:

1. **Given** 商品處於 Draft 狀態且賣家為擁有者，**When** 賣家發送上架請求，**Then** 商品狀態變更為 Active。
2. **Given** 商品不處於 Draft 狀態，**When** 賣家嘗試上架，**Then** 系統回傳衝突錯誤（409）。
3. **Given** 商品屬於其他賣家，**When** 當前用戶嘗試上架，**Then** 系統回傳禁止操作（403）。
4. **Given** Draft 商品的結標時間已過，**When** 賣家嘗試上架，**Then** 系統回傳驗證錯誤（422）。

---

### Edge Cases

- 賣家刪除帳號後，其 Draft 商品如何處理？（本 Phase 範圍外，商品保留，owner_id 為孤立參考）
- 結標時出現平手出價（同金額）？依先出價時間為準（最先出價者為 WinnerId）
- 分類可為多層級（Category 有 parentId），查詢清單時是否含子分類商品？本 Phase 僅依 categoryId 精確匹配
- 商品圖片 URL 格式驗證？僅驗證格式為有效 URL，不驗證可連線性
- 同時結標大量商品的效能問題？排程以批次處理，單次掃描上限為 100 筆

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: 已登入用戶 MUST 能建立 Draft 狀態的拍賣商品，欄位含標題（必填）、描述（選填）、起標價（必填，> 0）、結標時間（必填，> 現在）、分類 ID（選填）、圖片 URL 清單（選填，最多 5 張）。
- **FR-002**: 系統 MUST 驗證建立商品時的輸入規則：起標價必須大於 0；結標時間必須比現在晚至少 1 分鐘；圖片數量不得超過 5 張。違反規則回傳 422 Unprocessable Entity。
- **FR-003**: 擁有者 MUST 能修改自己的 Draft 狀態商品的所有欄位，同樣須通過 FR-002 驗證規則。
- **FR-004**: 系統 MUST 禁止非擁有者修改商品，回傳 403 Forbidden。
- **FR-005**: 系統 MUST 禁止修改非 Draft 狀態的商品，回傳 409 Conflict。
- **FR-006**: 擁有者 MUST 能將 Draft 商品上架，變更狀態為 Active。若結標時間已過則回傳 422。
- **FR-007**: 系統 MUST 提供商品列表查詢，支援以下篩選參數：`q`（關鍵字，匹配標題）、`category`（分類 ID）、`page`（頁碼，預設 1）、`pageSize`（每頁筆數，預設 20，最大 100）。
- **FR-008**: 系統 MUST 在商品列表回應中包含分頁資訊（totalCount、page、pageSize）。
- **FR-009**: 系統 MUST 提供商品詳情查詢，含所有欄位、圖片清單，以及目前最高出價金額（來自出價模組，若無出價則為 null）。
- **FR-010**: 已登入用戶 MUST 能將商品加入或移除個人追蹤清單，操作為冪等（重複加入/移除不報錯）。
- **FR-011**: 已登入用戶 MUST 能查詢自己的追蹤清單，顯示已追蹤商品的基本資訊。
- **FR-012**: 系統排程 MUST 每 60 秒掃描一次所有結標時間已到的 Active 商品，將其狀態變更為 Ended。
- **FR-013**: 商品結標且有出價紀錄時，系統 MUST 發布 AuctionWonEvent 至內部事件匯流排，事件內容含 AuctionId、WinnerId（最高出價者）、SoldAmount（最高出價金額）、SellerId（商品擁有者）。
- **FR-014**: 商品結標排程 MUST 為冪等，同一商品不得重複結標。
- **FR-015**: 商品狀態流轉 MUST 嚴格遵守 Draft → Active → Ended 單向狀態機，禁止跳轉或逆轉。

### Key Entities

- **Auction（拍賣商品）**: 核心實體，代表一筆拍賣，屬性含擁有者 ID（賣家）、標題、描述、起標價、結標時間、狀態（Draft/Active/Ended）、得標者 ID（結標後填入）、成交金額（結標後填入）。
- **AuctionImage（商品圖片）**: 商品附屬圖片，每筆商品最多 5 張，含圖片 URL 與顯示順序。
- **Category（分類）**: 商品分類，含名稱與父分類 ID（支援階層結構，但本 Phase 查詢以單層精確匹配為主）。
- **Watchlist（追蹤清單）**: 記錄用戶與商品的追蹤關係，含用戶 ID 與商品 ID，唯一約束防止重複。

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 賣家可在 3 分鐘內完成從建立商品到成功上架的完整流程。
- **SC-002**: 商品列表查詢（含分頁）在資料量 10,000 筆 Active 商品時，95% 的請求應在 500 毫秒內回應。
- **SC-003**: 排程結標誤差不超過 2 分鐘（結標時間到後最遲 2 分鐘內完成狀態變更）。
- **SC-004**: AuctionWonEvent 在結標後可被正確消費，下游模組（Ordering、Notification）能接收完整事件資料。
- **SC-005**: 追蹤清單的加入/移除操作冪等性達 100%（任何重複操作均不產生錯誤或重複資料）。
- **SC-006**: 所有輸入驗證錯誤均回傳清晰的錯誤訊息，指明違反的欄位與規則。

## Assumptions

- 商品列表預設只顯示 Active 狀態的商品（Draft 與 Ended 不出現在公開列表）。
- 分類資料（categories 表）由系統預先種入，本 Phase 不提供分類的 CRUD 管理功能。
- 最高出價查詢由出價模組（Bidding）提供介面，本 Phase 實作 null 佔位符（尚未有出價模組時回傳 null）。
- 商品圖片以 URL 形式輸入，本 Phase 不實作實際圖片上傳功能（Upload）。
- 結標排程在單一實例環境執行，本 Phase 不考慮多實例部署的分散式鎖定問題。
