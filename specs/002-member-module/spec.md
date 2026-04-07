# Feature Specification: Member 模組 — 註冊、驗證與個人資料

**Feature Branch**: `002-member-module`  
**Created**: 2026-04-08  
**Status**: Draft  
**Input**: User description: "實作 Member 模組：使用者以 email + username + password + address + phone number 註冊帳號（重複 email 回 409）、以 email + password 登入取得 JWT Access Token（15分鐘）及 Refresh Token（7天）、使用 Refresh Token 換取新 Access Token（Token Rotation）、登出（使 Refresh Token 失效）、查詢自己的個人資料、更新個人資料（username、顯示名稱），密碼使用 BCrypt 雜湊儲存，schema: member，tables: users, refresh_tokens"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - 新訪客完成帳號註冊 (Priority: P1)

新訪客希望透過填寫身份與聯絡資訊建立帳號，以便參與競標活動。

**Why this priority**: 註冊是平台所有功能的入口點。沒有帳號，任何其他功能均無法使用。單獨交付此功能即可為系統提供完整的身份識別基礎。

**Independent Test**: 可透過「提交有效的註冊資料並確認帳號建立成功，再用相同 email 再次註冊確認收到 409」獨立驗證，不需要其他功能即可交付價值。

**Acceptance Scenarios**:

1. **Given** 訪客提供唯一的 email、username、password、address、phone number，**When** 提交註冊，**Then** 帳號成功建立，回傳 201 及帳號基本資料
2. **Given** 訪客提供的 email 已存在於系統，**When** 嘗試註冊，**Then** 回傳 409 Conflict，錯誤訊息為 `"Email already registered"`
3. **Given** 訪客提供的密碼不符合複雜度規則（少於 8 字元，或缺少大小寫字母或數字），**When** 提交註冊，**Then** 回傳 422，並明確指出哪個欄位不符合規則
4. **Given** 訪客提供格式不正確的 email，**When** 提交註冊，**Then** 回傳 422，指出 email 欄位格式錯誤

---

### User Story 2 - 已註冊使用者登入取得驗證憑證 (Priority: P1)

已註冊的使用者希望以 email 和密碼登入，取得驗證憑證以存取需要身份驗證的功能。

**Why this priority**: 登入是所有已驗證操作的前提條件。沒有登入能力，已註冊的使用者無法與平台互動。

**Independent Test**: 可透過「先完成 User Story 1 註冊，再立即登入，確認收到 token，並用該 token 呼叫受保護的端點」獨立驗證。

**Acceptance Scenarios**:

1. **Given** 已註冊使用者提供正確的 email 與密碼，**When** 提交登入，**Then** 回傳 Access Token（有效期 15 分鐘）與 Refresh Token（有效期 7 天）
2. **Given** 已註冊使用者提供錯誤的密碼，**When** 嘗試登入，**Then** 回傳 401 Unauthorized（不指出是密碼還是 email 錯誤，防止帳號列舉攻擊）
3. **Given** 系統中不存在的 email，**When** 嘗試登入，**Then** 回傳 401 Unauthorized

---

### User Story 3 - 使用者以 Refresh Token 換取新的 Access Token (Priority: P2)

已登入使用者的 Access Token 過期後，希望透過 Refresh Token 取得新的 Access Token，不需要重新登入。

**Why this priority**: Token Rotation 對於安全且流暢的工作階段體驗至關重要。沒有此功能，使用者每 15 分鐘就需要重新登入。

**Independent Test**: 可透過「登入後模擬 token 過期，提交 Refresh Token 換取新 token，確認舊 Refresh Token 已失效」獨立驗證。

**Acceptance Scenarios**:

1. **Given** 有效且未撤銷的 Refresh Token，**When** 提交換取請求，**Then** 回傳新的 Access Token 與新的 Refresh Token，原 Refresh Token 立即失效（Token Rotation）
2. **Given** 已過期的 Refresh Token，**When** 提交換取請求，**Then** 回傳 401 Unauthorized
3. **Given** 已撤銷的 Refresh Token（已使用過或來自登出操作），**When** 提交換取請求，**Then** 回傳 401 Unauthorized

---

### User Story 4 - 使用者登出並使 Refresh Token 失效 (Priority: P2)

已登入使用者希望登出，使其 Refresh Token 失效以安全地結束工作階段。

**Why this priority**: 安全登出可防止使用者結束工作階段後 token 被重複使用，對共用裝置場景尤為重要。

**Independent Test**: 可透過「登入後登出，再以舊 Refresh Token 嘗試換取新 token，確認收到 401」獨立驗證。

**Acceptance Scenarios**:

1. **Given** 有效的 Refresh Token，**When** 提交登出請求，**Then** 該 token 被撤銷，回傳 204 No Content
2. **Given** 已撤銷的 Refresh Token，**When** 再次提交登出請求，**Then** 仍回傳 204 No Content（冪等，不回傳錯誤）

---

### User Story 5 - 已登入使用者查詢自己的個人資料 (Priority: P3)

已登入的使用者希望查詢自己的帳號資料，確認各欄位內容是否正確。

**Why this priority**: 個人資料查詢是基礎的自助服務功能，但不影響核心競標參與流程。

**Independent Test**: 可透過「登入後取得個人資料，確認所有欄位均正確回傳」獨立驗證。

**Acceptance Scenarios**:

1. **Given** 有效的 Access Token，**When** 請求個人資料，**Then** 回傳 id、email、username、顯示名稱、地址、電話號碼、角色等欄位
2. **Given** 無有效 Access Token（未提供或已過期），**When** 請求個人資料，**Then** 回傳 401 Unauthorized

---

### User Story 6 - 已登入使用者更新個人資料 (Priority: P3)

已登入的使用者希望更新 username 與顯示名稱，使個人資料保持最新。

**Why this priority**: 個人資料編輯可提升使用者體驗，但不是競標參與的必要條件。

**Independent Test**: 可透過「登入後更新 username 與顯示名稱，再查詢個人資料確認變更已生效」獨立驗證。

**Acceptance Scenarios**:

1. **Given** 有效的 Access Token 與合法的欄位值，**When** 提交個人資料更新，**Then** 回傳更新後的個人資料，包含新的 username 與顯示名稱
2. **Given** 有效的 Access Token 但必填欄位空白或無效，**When** 提交更新，**Then** 回傳 422 並指出哪個欄位不符規則
3. **Given** 更新的 username 與其他使用者重複，**When** 提交更新，**Then** 回傳 409 Conflict
4. **Given** 無有效 Access Token，**When** 嘗試更新個人資料，**Then** 回傳 401 Unauthorized

---

### Edge Cases

- Refresh Token 被同時使用兩次（競爭條件）時，只有第一次成功；第二次因 Token Rotation 已撤銷而回傳 401
- 地址欄位超過長度上限（500 字元）時，回傳 422 並指明 address 欄位
- 電話號碼包含非法字元（不允許 `+`、`-`、空格以外的非數字字元）時，回傳 422 並指明 phone 欄位
- 使用者嘗試透過此端點查詢其他人的個人資料：系統依據 Access Token 中的身份僅回傳請求者本人的資料，無法查詢他人
- 帳號建立後，顯示名稱為空時預設使用 username 作為顯示名稱

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: 系統 MUST 允許新訪客以 email、username、password、address、phone number 建立帳號
- **FR-002**: 系統 MUST 在 email 已存在時拒絕註冊，回傳 409 Conflict，訊息為 `"Email already registered"`
- **FR-003**: 系統 MUST 強制執行密碼複雜度規則：至少 8 字元，包含至少一個大寫字母、一個小寫字母、一個數字
- **FR-004**: 系統 MUST 以不可逆雜湊演算法儲存密碼，任何情況下都 MUST NOT 儲存或回傳明文密碼
- **FR-005**: 系統 MUST 允許已註冊使用者以 email + password 登入，成功時回傳有效期 15 分鐘的 Access Token 與有效期 7 天的 Refresh Token
- **FR-006**: 系統 MUST NOT 在登入失敗時指出是密碼錯誤還是 email 不存在；兩種情況均回傳相同的 401 回應，防止帳號列舉攻擊
- **FR-007**: 系統 MUST 允許持有有效 Refresh Token 的使用者換取新的 Access Token 與 Refresh Token（Token Rotation），並立即撤銷已提交的 Refresh Token
- **FR-008**: 系統 MUST 在 Refresh Token 已過期或已撤銷時拒絕換取，回傳 401
- **FR-009**: 系統 MUST 允許已登入使用者透過提交 Refresh Token 登出，回傳 204；以已撤銷的 token 重複登出 MUST 仍回傳 204（冪等）
- **FR-010**: 系統 MUST 允許已登入使用者查詢自己的個人資料，包含：id、email、username、顯示名稱、地址、電話號碼、角色、建立時間
- **FR-011**: 系統 MUST 允許已登入使用者更新 username 與顯示名稱；若 username 與他人重複 MUST 回傳 409
- **FR-012**: 系統 MUST 為所有新註冊使用者預設指派 `Member` 角色

### Key Entities

- **使用者（User）**: 代表已註冊的平台參與者。屬性：唯一 id、email（唯一）、username（唯一）、雜湊密碼、地址（自由文字，上限 500 字元）、電話號碼（支援國際格式）、顯示名稱（選填，上限 50 字元，預設為 username）、角色（Member / Admin，預設 Member）、建立時間戳記、最後更新時間戳記
- **Refresh Token**: 代表一次已發行的工作階段憑證。屬性：唯一 id、關聯的使用者 id、token 摘要（非原始 token）、到期時間戳記、撤銷旗標、發行時間戳記。同一使用者可同時持有多個有效 token（例如多裝置登入），各自獨立管理

---

## Clarifications

### Session 2026-04-08

- Q: address 是否需要拆分成結構化欄位（城市、郵遞區號等）？ → A: 本期以自由文字儲存，不拆分子欄位；結構化地址管理留待後續迭代
- Q: 電話號碼的驗證規則為何？ → A: 接受國際格式（`+886-912-345-678`、`0912345678`），允許 `+`、`-`、空格；電信業者層級驗證不在範圍內
- Q: 顯示名稱（display_name）是否必填？ → A: 選填；未提供時預設使用 username 作為顯示名稱
- Q: 一個使用者是否可同時在多裝置保有多個有效 Refresh Token？ → A: 是，各裝置的 token 各自獨立管理，登出只撤銷提交的那一個 token
- Q: Access Token 的 payload 需包含哪些 claims？ → A: 至少包含 sub（使用者 id）、email、role

---

## Assumptions

- 地址以自由文字儲存，不拆分結構化子欄位（城市、郵遞區號等）
- 電話號碼接受國際格式（`+886-912-345-678`、`0912345678`）；電信業者層級驗證不在範圍內
- 顯示名稱選填；未提供時預設值為 username
- 同一使用者可在多裝置同時保有多個有效 Refresh Token，各自獨立管理
- `Admin` 角色存在於領域模型中，但提升使用者為 Admin 的管理功能不在本期範圍內
- Refresh Token 以 SHA-256 摘要儲存於資料庫；原始 token 僅在發行時回傳一次，不以明文儲存
- Access Token payload 至少包含 sub（使用者 id）、email、role
- username 全域唯一，更新時若與他人重複回傳 409

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 新使用者從開始填寫到帳號建立完成的流程可在 2 分鐘內完成
- **SC-002**: 已註冊使用者在正常網路條件下可在 3 秒內完成登入並收到有效 token
- **SC-003**: 100% 的重複 email 註冊請求被即時拒絕，並回傳清楚且可操作的錯誤訊息
- **SC-004**: 已過期、已撤銷或重放的 Refresh Token 在 1 秒內被一致拒絕，不存在部分成功的認證升級
- **SC-005**: 已登入使用者查詢或更新個人資料後，變更在同一次回應中即時反映
- **SC-006**: 密碼複雜度違規在註冊時即被攔截並明確告知，降低因密碼過於簡單導致的後續問題
- **SC-007**: 所有 token 操作（發行、輪換、撤銷）產生一致的結果，不存在部分狀態——操作完整成功或系統保持不變
