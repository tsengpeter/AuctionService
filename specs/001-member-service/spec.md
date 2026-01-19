# 功能規格：會員服務

**功能分支**: `001-member-service`  
**建立日期**: 2025-11-04  
**狀態**: 草稿  
**輸入**: 使用者描述："實作會員服務，負責使用者註冊、登入、身份驗證與個人資料管理"



**注意**: 根據專案準則，本文件必須使用繁體中文撰寫。

## 澄清事項

### Session 2025-11-06

- Q: 當使用者在多個裝置同時登入，一個裝置變更密碼後，其他裝置的 JWT 與 Refresh Token 如何處理？ → A: 立即撤銷所有現有的 Refresh Token（強制所有裝置重新登入）
- Q: 使用者名稱允許哪些字元類型？ → A: 僅允許字母（英文、中文等語言字元）和空格，不允許數字、底線、連字號（避免建立子帳號形式如 john01）

### Session 2025-11-14

- Q: 密碼雜湊演算法要使用哪一種 (PBKDF2/bcrypt/Argon2)? → A: bcrypt，並將密碼與雪花ID組合後雜湊 (bcrypt(password + snowflakeId))，提供額外的安全層
- Q: 使用者主鍵 (ID) 使用何種生成方式? → A: 雪花ID (Snowflake ID, 64-bit Long)，確保分散式環境下的唯一性與高效能
- Q: 雪花ID使用自行實作還是現成套件? → A: 使用成熟的 .NET 套件 (如 IdGen 或 Snowflake.Core)，避免實作錯誤並減少維護成本
- Q: 電子郵件唯一性驗證的訊息揭露策略? → A: 註冊時明確告知「此電子郵件已被使用」，登入時使用模糊訊息「電子郵件或密碼錯誤」(平衡安全性與使用者體驗)
- Q: JWT 存取權杖的有效期限? → A: 15 分鐘 (業界標準，平衡安全性與使用者體驗)
- Q: JWT 使用哪種演算法 (HS256 對稱金鑰 vs RS256 非對稱金鑰)? → A: HS256 對稱金鑰 (HMAC-SHA256，驗證快速，適合內部微服務)

### Session 2026-01-14

- Q: 忘記密碼功能的驗證方式? → A: 支援電子郵件和手機簡訊兩種驗證方式，使用者可選擇任一方式
- Q: 註冊時是否需要手機號碼? → A: 是，電子郵件和手機號碼都是必填
- Q: 驗證碼設定? → A: 6位數字、有效期5分鐘、最多錯誤3次、發送冷卻時間60秒、儲存於Redis並使用TTL自動清除
- Q: 通知服務整合? → A: 優先使用雲端服務(AWS SES + AWS SNS)，次之 SMTP + Twilio
- Q: 密碼重設後的安全措施? → A: 撤銷所有現有的 Refresh Token，強制所有裝置重新登入
- Q: 驗證功能的設計? → A: 驗證碼儲存在 Redis 中，使用 TTL 機制自動清除過期或失效的驗證碼；驗證成功後更新 User 表的驗證狀態欄位

### Session 2026-01-15

- Q: 註冊與驗證的關係? → A: 註冊立即成功，使用者可以馬上登入使用系統；驗證是獨立的功能，可在註冊後任何時候執行
- Q: 未驗證帳號的限制? → A: 未驗證帳號可以登入和使用基本功能，但可能有業務限制（例如：競標金額上限、無法發起拍賣等）
- Q: 驗證狀態如何記錄? → A: User 資料表有 EmailVerified 和 PhoneNumberVerified 兩個布林欄位，預設為 false
- Q: 驗證流程? → A: 使用者可透過獨立的驗證端點請求驗證碼，驗證成功後更新對應的驗證狀態欄位
- Q: 跨微服務如何檢查驗證狀態? → A: 採用混合方案：(1) JWT payload 包含驗證狀態快照 (2) 其他微服務調用 `/api/auth/validate` 取得最新驗證狀態 (3) 各微服務根據業務需求決定是否要求驗證
- Q: 驗證狀態的更新時機? → A: JWT 中的驗證狀態在簽發時固定，但用戶完成驗證後，下次使用 Refresh Token 更新 JWT 時會更新；`/api/auth/validate` 端點始終返回資料庫中的最新狀態

## User Scenarios & Testing *(mandatory)*



## 使用者情境與測試 *(必填)*<!--

  IMPORTANT: User stories should be PRIORITIZED as user journeys ordered by importance.

### 使用者故事 1 - 讓使用者註冊與登入 (優先順序: P1)  Each user story/journey must be INDEPENDENTLY TESTABLE - meaning if you implement just ONE of them,

  you should still have a viable MVP (Minimum Viable Product) that delivers value.

新使用者透過提供電子郵件、手機號碼、密碼與使用者名稱提交註冊申請，系統建立待驗證帳號並同時發送驗證碼至電子郵件和手機號碼，使用者必須成功驗證兩個驗證碼才能完成註冊，完成註冊後方可登入系統取得存取權杖。未完成驗證的帳號無法登入，且處於待驗證狀態。  

  Assign priorities (P1, P2, P3, etc.) to each story, where P1 is the most critical.

**優先順序理由**: 這是會員服務的核心價值，沒有註冊與登入功能，其他所有功能都無法使用。電子郵件和手機號碼的雙重驗證確保帳號安全性和真實性。這是最小可行產品（MVP）的基礎。  Think of each story as a standalone slice of functionality that can be:

  - Developed independently

**獨立測試**: 可透過提供有效的註冊資訊（電子郵件、手機號碼、密碼、使用者名稱）提交註冊，驗證系統發送雙驗證碼，然後輸入正確的驗證碼完成註冊。註冊完成後再使用帳號密碼登入並獲取 JWT tokens，完整測試註冊、驗證與登入流程。  - Tested independently

  - Deployed independently

**驗收情境**:

1. **Given** 使用者尚未註冊, **When** 使用者提供有效的電子郵件、手機號碼、密碼（8 字元以上）與使用者名稱提交註冊, **Then** 系統立即建立帳號（EmailVerified=false, PhoneNumberVerified=false）並回傳註冊成功訊息與使用者資訊

2. **Given** 使用者已註冊成功, **When** 使用者使用電子郵件與密碼登入, **Then** 系統驗證成功並回傳 JWT 存取權杖與 Refresh Token

3. **Given** 電子郵件已被註冊, **When** 使用者嘗試使用相同電子郵件註冊, **Then** 系統拒絕註冊並顯示「此電子郵件已被使用」錯誤訊息

4. **Given** 手機號碼已被註冊, **When** 使用者嘗試使用相同手機號碼註冊, **Then** 系統拒絕註冊並顯示「此手機號碼已被使用」錯誤訊息

5. **Given** 使用者提供的密碼少於 8 字元, **When** 使用者嘗試註冊, **Then** 系統拒絕註冊並顯示「密碼必須至少 8 個字元」錯誤訊息

6. **Given** 使用者提供無效的電子郵件格式, **When** 使用者嘗試註冊, **Then** 系統拒絕註冊並顯示「請提供有效的電子郵件地址」錯誤訊息

7. **Given** 使用者提供無效的手機號碼格式（非 E.164 格式）, **When** 使用者嘗試註冊, **Then** 系統拒絕註冊並顯示「請提供有效的手機號碼（+國碼加號碼）」錯誤訊息

8. **Given** 使用者已註冊, **When** 使用者使用錯誤的密碼登入, **Then** 系統拒絕登入並顯示「電子郵件或密碼錯誤」訊息**Why this priority**: [Explain the value and why it has this priority level]



---**Independent Test**: [Describe how this can be tested independently - e.g., "Can be fully tested by [specific action] and delivers [specific value]"]



### 使用者故事 2 - Refresh Token 更新 (優先順序: P2)**Acceptance Scenarios**:



使用者的 JWT 存取權杖過期後，可使用 Refresh Token 換取新的 JWT，無需重新輸入密碼，提供流暢的使用體驗。1. **Given** [initial state], **When** [action], **Then** [expected outcome]

2. **Given** [initial state], **When** [action], **Then** [expected outcome]

**優先順序理由**: JWT 有效期限僅 15 分鐘，若無 Refresh Token 更新機制，使用者需頻繁重新登入，嚴重影響使用體驗。此功能是基本身份驗證流程的重要補充。

---

**獨立測試**: 可透過取得有效的 Refresh Token，模擬 JWT 過期情境，驗證系統能成功換發新的 JWT，確保使用者在 Refresh Token 有效期限（7 天）內無需重新登入。

### User Story 2 - [Brief Title] (Priority: P2)

**驗收情境**:

[Describe this user journey in plain language]

1. **Given** 使用者持有有效的 Refresh Token, **When** 使用者使用 Refresh Token 請求新的 JWT, **Then** 系統驗證 Refresh Token 並回傳新的 JWT 存取權杖

2. **Given** Refresh Token 已過期, **When** 使用者使用過期的 Refresh Token 請求新的 JWT, **Then** 系統拒絕請求並回傳「請重新登入」訊息**Why this priority**: [Explain the value and why it has this priority level]

3. **Given** Refresh Token 已被撤銷, **When** 使用者使用被撤銷的 Refresh Token, **Then** 系統拒絕請求並回傳「權杖無效，請重新登入」訊息

**Independent Test**: [Describe how this can be tested independently]

---

### 使用者故事 2.5 - JWT Token 驗證 (優先順序: P1)

其他微服務（如 BiddingService、AuctionService）需要驗證從客戶端傳來的 JWT Token 是否有效，並獲取對應的使用者資訊，以確保只有已認證的使用者才能執行特定操作。

**優先順序理由**: 這是微服務架構中服務間通訊的基礎功能。其他服務（如競標服務）需要依賴此功能來驗證使用者身份，確保系統安全性。沒有此功能，其他服務無法驗證請求的合法性。

**獨立測試**: 可透過提供有效的 JWT Token 呼叫驗證端點（`GET /api/auth/validate?token=xxx`），驗證系統能正確解析 Token 並回傳使用者資訊；使用過期或無效的 Token 驗證系統能正確拒絕請求。

**驗收情境**:

1. **Given** 使用者持有有效的 JWT Token, **When** 其他服務呼叫 Token 驗證端點（`GET /api/auth/validate?token=xxx`）, **Then** 系統回傳 200 OK 與 Token 有效狀態、使用者 ID（userId）及過期時間（expiresAt）

2. **Given** JWT Token 已過期, **When** 其他服務呼叫 Token 驗證端點, **Then** 系統回傳 401 Unauthorized 與 isValid=false

3. **Given** JWT Token 格式無效或簽章錯誤, **When** 其他服務呼叫 Token 驗證端點, **Then** 系統回傳 401 Unauthorized 與 isValid=false

4. **Given** 請求未包含 token query parameter, **When** 其他服務呼叫 Token 驗證端點, **Then** 系統回傳 400 Bad Request 並提示「Token parameter is required」

5. **Given** token query parameter 為空字串, **When** 其他服務呼叫 Token 驗證端點, **Then** 系統回傳 400 Bad Request 並提示「Token parameter is required」

---

### 使用者故事 2.6 - 電子郵件與手機號碼驗證 (優先順序: P2)

使用者在註冊後可以透過獨立的驗證功能來驗證電子郵件和手機號碼。系統發送驗證碼到使用者的電子郵件或手機，使用者輸入正確的驗證碼後，系統更新對應的驗證狀態欄位（EmailVerified 或 PhoneNumberVerified）。未驗證的帳號可以正常登入和使用基本功能，但可能在某些業務場景下有限制（例如：競標金額上限、無法發起拍賣等）。

**優先順序理由**: 驗證功能確保使用者提供的聯絡方式真實有效，提高帳號安全性和信任度。雖然不阻擋基本使用，但對於高價值交易場景（如拍賣）是重要的信任機制。

**獨立測試**: 可透過已註冊但未驗證的帳號，請求發送驗證碼到電子郵件或手機號碼，然後輸入正確的驗證碼，驗證系統正確更新驗證狀態欄位。

**驗收情境**:

1. **Given** 使用者已登入但電子郵件未驗證（EmailVerified=false）, **When** 使用者請求發送電子郵件驗證碼, **Then** 系統發送 6 位數驗證碼至使用者的電子郵件

2. **Given** 使用者已請求電子郵件驗證碼, **When** 使用者輸入正確的驗證碼, **Then** 系統標記 EmailVerified=true 並回傳驗證成功訊息

3. **Given** 使用者已登入但手機號碼未驗證（PhoneNumberVerified=false）, **When** 使用者請求發送手機驗證碼, **Then** 系統發送 6 位數驗證碼至使用者的手機號碼

4. **Given** 使用者已請求手機驗證碼, **When** 使用者輸入正確的驗證碼, **Then** 系統標記 PhoneNumberVerified=true 並回傳驗證成功訊息

5. **Given** 使用者輸入錯誤的驗證碼, **When** 錯誤次數超過 3 次, **Then** 系統使該驗證碼失效並要求重新發送

6. **Given** 驗證碼已發送, **When** 超過 5 分鐘未使用, **Then** 系統使該驗證碼自動過期（Redis TTL 清除）

7. **Given** 驗證碼已發送, **When** 使用者在 60 秒冷卻時間內再次請求發送, **Then** 系統拒絕並回傳剩餘等待時間

8. **Given** 使用者的電子郵件或手機已驗證, **When** 使用者再次請求驗證, **Then** 系統回傳「該項目已驗證」訊息

---

**Acceptance Scenarios**:

### 使用者故事 3 - 個人資料查詢 (優先順序: P2)

1. **Given** [initial state], **When** [action], **Then** [expected outcome]

使用者可查詢自己的完整個人資料（包含電子郵件、使用者名稱等），也可查詢其他使用者的公開資料（僅使用者名稱），用於顯示商品賣家資訊等場景。

---

**優先順序理由**: 個人資料查詢是帳號管理的基礎功能，同時支援其他服務（如拍賣服務）顯示賣家資訊的需求。

### User Story 3 - [Brief Title] (Priority: P3)

**獨立測試**: 可透過已登入的使用者查詢自己的資料，並查詢其他使用者的公開資料，驗證系統正確區分私密資訊與公開資訊。

[Describe this user journey in plain language]

**驗收情境**:

**Why this priority**: [Explain the value and why it has this priority level]

1. **Given** 使用者已登入, **When** 使用者查詢自己的個人資料, **Then** 系統回傳完整個人資料（電子郵件、使用者名稱、建立時間、更新時間）

2. **Given** 使用者已登入, **When** 使用者查詢其他使用者的資料, **Then** 系統僅回傳公開資料（使用者名稱、建立時間），不包含電子郵件等敏感資訊**Independent Test**: [Describe how this can be tested independently]

3. **Given** 使用者未登入, **When** 嘗試查詢任何使用者資料, **Then** 系統拒絕請求並回傳「需要登入」訊息

4. **Given** 查詢的使用者 ID 不存在, **When** 使用者查詢該 ID, **Then** 系統回傳「使用者不存在」訊息**Acceptance Scenarios**:



---1. **Given** [initial state], **When** [action], **Then** [expected outcome]



### 使用者故事 4 - 個人資料更新與密碼變更 (優先順序: P3)---



使用者可更新自己的使用者名稱與電子郵件，也可變更密碼以維護帳號安全。[Add more user stories as needed, each with an assigned priority]



**優先順序理由**: 資料更新與密碼變更是帳號管理的輔助功能，雖然重要但非系統初期運作的必要條件。

**獨立測試**: 可透過已登入的使用者更新資料欄位，驗證系統正確更新並驗證資料唯一性；透過密碼變更流程驗證舊密碼驗證與新密碼設定。

**驗收情境**:

1. **Given** 使用者已登入, **When** 使用者更新使用者名稱, **Then** 系統成功更新並回傳更新後的資料

2. **Given** 使用者已登入, **When** 使用者更新電子郵件為未被使用的新電子郵件, **Then** 系統成功更新- What happens when [boundary condition]?

3. **Given** 使用者已登入, **When** 使用者更新電子郵件為已被其他使用者使用的電子郵件, **Then** 系統拒絕更新並顯示「此電子郵件已被使用」訊息- How does system handle [error scenario]?

4. **Given** 使用者已登入, **When** 使用者提供正確的舊密碼與新密碼（8 字元以上）進行密碼變更, **Then** 系統成功更新密碼

5. **Given** 使用者已登入, **When** 使用者提供錯誤的舊密碼進行密碼變更, **Then** 系統拒絕變更並顯示「舊密碼錯誤」訊息## Requirements *(mandatory)*

6. **Given** 使用者已登入, **When** 使用者提供的新密碼少於 8 字元, **Then** 系統拒絕變更並顯示「密碼必須至少 8 個字元」訊息

---

### 使用者故事 4.5 - 更新電子郵件與手機號碼 (優先順序: P3)

**狀態**: 未來功能，規劃中

使用者可以更新自己的電子郵件或手機號碼，更新時需要驗證新的電子郵件或手機號碼以確保有效性。此功能重用 VerificationToken 通用驗證機制。

**優先順序理由**: 雖非核心功能，但對於使用者維護帳號資訊很重要。採用通用驗證機制後，實作成本降低。

**驗收情境**:

1. **Given** 使用者已登入, **When** 使用者提交新的電子郵件地址, **Then** 系統發送 6 位數驗證碼到新的電子郵件（類型：EmailUpdate）

2. **Given** 使用者收到電子郵件驗證碼, **When** 使用者輸入正確的驗證碼, **Then** 系統更新電子郵件並標記為已驗證

3. **Given** 使用者已登入, **When** 使用者提交新的手機號碼, **Then** 系統發送 6 位數驗證碼到新的手機號碼（類型：PhoneUpdate）

4. **Given** 使用者收到手機驗證碼, **When** 使用者輸入正確的驗證碼, **Then** 系統更新手機號碼並標記為已驗證

**技術說明**: 使用 VerificationToken 實體，VerificationType 為 EmailUpdate 或 PhoneUpdate，復用現有驗證邏輯。

---

### 使用者故事 5 - 忘記密碼與密碼重設 (優先順序: P2)

使用者忘記密碼時，可選擇透過已註冊的電子郵件或手機號碼接收驗證碼，驗證身份後自助重設新密碼，無需聯繫客服即可快速恢復帳號存取權限。

**優先順序理由**: 忘記密碼是使用者常見痛點，缺少此功能會導致使用者流失和增加客服負擔。雖非 MVP 必要功能，但對使用者體驗和運營效率至關重要，應在基礎身份驗證功能完成後優先實作。

**獨立測試**: 可透過完整的密碼重設流程測試（請求驗證碼 → 驗證碼送達 → 輸入驗證碼 → 設定新密碼 → 使用新密碼登入），驗證系統能正確處理電子郵件和簡訊兩種驗證方式，並在密碼重設後撤銷所有現有 Refresh Token。

**驗收情境**:

1. **Given** 使用者忘記密碼且已註冊電子郵件, **When** 使用者選擇「電子郵件驗證」並輸入 email, **Then** 系統發送 6 位數驗證碼到該 email（有效期 5 分鐘，Redis 儲存）

2. **Given** 使用者忘記密碼且已註冊手機號碼, **When** 使用者選擇「簡訊驗證」並輸入手機號碼, **Then** 系統發送 6 位數驗證碼到該手機（有效期 5 分鐘，Redis 儲存）

3. **Given** 使用者收到驗證碼, **When** 使用者輸入正確的驗證碼和新密碼（8 字元以上）, **Then** 系統更新密碼並撤銷所有現有 Refresh Token，強制所有裝置重新登入

4. **Given** 驗證碼已過期（超過 5 分鐘，Redis 自動清除）, **When** 使用者嘗試使用過期驗證碼, **Then** 系統拒絕並提示「驗證碼已過期，請重新獲取」

5. **Given** 使用者輸入錯誤驗證碼, **When** 使用者連續錯誤 3 次, **Then** 系統鎖定該驗證碼並要求重新發送新的驗證碼

6. **Given** 使用者剛發送過驗證碼（60 秒內）, **When** 使用者嘗試再次請求驗證碼, **Then** 系統拒絕並提示「請在 X 秒後重試」（防止濫發）

7. **Given** 電子郵件或手機號碼不存在於系統中, **When** 使用者嘗試請求驗證碼, **Then** 系統回傳成功訊息（不洩漏帳號存在性，安全考量）

8. **Given** 使用者成功重設密碼, **When** 使用者嘗試使用舊密碼登入, **Then** 系統拒絕登入並提示「電子郵件或密碼錯誤」

9. **Given** 使用者成功重設密碼, **When** 使用者使用新密碼登入, **Then** 系統驗證成功並回傳新的 JWT 存取權杖與 Refresh Token

<!--

---  ACTION REQUIRED: The content in this section represents placeholders.

  Fill them out with the right functional requirements.

### 邊界情況

- 當 1 分鐘內來自同一 IP 的註冊請求超過 10 次時，系統應如何保護？（建議實作：返回 429 Too Many Requests 並要求等待 5 分鐘）
- 當使用者忘記密碼時，如何重設密碼？（目前規格未涵蓋此功能）
- 註冊功能仍可被用於枚舉已註冊的電子郵件（安全性與使用者體驗的取捨，優先使用者體驗）
- 登入功能不洩漏帳號存在資訊（統一回傳模糊錯誤訊息）

## 需求 *(必填)*



### 功能需求*Example of marking unclear requirements:*



### 功能需求*Example of marking unclear requirements:*

- **FR-000**: 系統必須使用雪花ID (Snowflake ID, 64-bit Long) 作為使用者的主鍵
- **FR-000-1**: 系統必須使用成熟的 .NET 雪花ID套件 (如 IdGen 或 Snowflake.Core) 生成唯一識別碼
- **FR-000-2**: 雪花ID生成器必須配置 Worker ID 與 Datacenter ID 以確保分散式環境下的唯一性

- **FR-001**: 系統必須允許使用者提供電子郵件、手機號碼、密碼與使用者名稱進行註冊
- **FR-001-0**: 系統必須在註冊時檢查電子郵件和手機號碼是否已被使用，若已存在則拒絕註冊並回傳明確錯誤訊息
- **FR-001-1**: 系統必須在註冊成功後立即建立帳號並標記 EmailVerified 和 PhoneNumberVerified 為 false
- **FR-001-2**: 系統必須在註冊成功後回傳註冊成功訊息與使用者資訊，但不包含 JWT tokens
- **FR-001-3**: 系統必須允許未驗證的帳號登入系統，但未驗證帳號可能在某些業務場景下有功能限制
- **FR-002**: 系統必須驗證電子郵件格式符合標準格式（包含 @ 符號與有效網域）
- **FR-002-1**: 系統必須驗證手機號碼符合 E.164 格式（+[國碼][號碼]，例如 +886912345678）
- **FR-002-2**: 系統必須提供獨立的驗證功能，允許已登入的使用者請求發送驗證碼至電子郵件或手機號碼
- **FR-002-3**: 系統必須生成 6 位數驗證碼，有效期限5分鐘，儲存在 Redis 並使用 TTL 自動清除
- **FR-002-4**: 系統必須允許每個驗證碼最多 3 次錯誤嘗試，超過後使驗證碼失效
- **FR-002-5**: 系統必須在發送驗證碼後 60 秒內拒絕重新發送（冷卻時間）
- **FR-002-6**: 系統必須在重新發送驗證碼時，將舊驗證碼標記為失效（IsUsed=true），確保同一時間只有一組驗證碼有效
- **FR-002-7**: 系統必須在使用者於冷卻時間內重複請求驗證碼時，回傳「VERIFICATION_CODE_COOLDOWN」錯誤並包含剩餘等待時間（remainingSeconds）
- **FR-002-8**: 系統必須允許使用者在驗證碼有效期內（5分鐘）但超過冷卻時間（60秒）後重新提交註冊，此時發送新的驗證碼並從 Redis 刪除舊驗證碼
- **FR-002-9**: 系統必須使用通用 VerificationToken 實體支援多種驗證類型（EmailVerification, PhoneVerification, PasswordReset, EmailUpdate, PhoneUpdate）

- **FR-003**: 系統必須驗證電子郵件的唯一性，不允許重複註冊
- **FR-003-0**: 系統必須驗證手機號碼的唯一性，不允許重複註冊
- **FR-003-1**: 系統必須驗證使用者名稱僅包含字母（英文、繁體中文等語言字元）和空格，不允許數字、底線、連字號等特殊字元
- **FR-003-2**: 系統必須驗證使用者名稱長度為 3-50 字元（中文字元計為 1 字元）
- **FR-004**: 系統必須驗證密碼符合強密碼規則：
  - 長度至少 8 個字元
  - 必須包含至少一個大寫字母 (A-Z)
  - 必須包含至少一個小寫字母 (a-z)
  - 必須包含至少一個數字 (0-9)
  - 必須包含至少一個特殊符號 (非英數字元)
- **FR-005**: 系統必須將密碼加密儲存，不可儲存明文密碼
- **FR-005-1**: 系統必須使用 bcrypt 演算法進行密碼雜湊
- **FR-005-2**: 系統必須將密碼與使用者的雪花ID組合後進行雜湊: bcrypt(password + snowflakeId)，提供額外的安全保護
- **FR-005-3**: bcrypt 的 work factor (成本因子) 建議設定為 12（可根據安全需求調整）

- **FR-006**: 系統必須在註冊成功後回傳註冊成功訊息與使用者資訊（不包含 JWT tokens，使用者需要再次調用登入端點）
- **FR-007**: 系統必須允許使用者使用電子郵件與密碼進行登入

- **FR-008**: 系統必須在登入成功後回傳 JWT 存取權杖（有效期限 15 分鐘）與 Refresh Token（有效期限 7 天）
- **FR-008-1**: JWT 必須使用 HS256 對稱金鑰演算法 (HMAC-SHA256) 進行簽章
- **FR-008-2**: JWT 密鑰必須透過環境變數或密鑰管理服務取得，不可寫入程式碼或設定檔
- **FR-008-3**: JWT 的 payload (claims) 必須包含以下資訊：userId, email, username, emailVerified, phoneNumberVerified
- **FR-008-4**: JWT 中的驗證狀態（emailVerified, phoneNumberVerified）為簽發時的快照，最新狀態應透過 Token 驗證 API 查詢

- **FR-009**: 系統必須在登入失敗時顯示明確的錯誤訊息（但不洩漏帳號是否存在的資訊）
- **FR-009-1**: 系統必須在登入失敗時（無論是電子郵件不存在或密碼錯誤）統一回傳「電子郵件或密碼錯誤」訊息
- **FR-009-2**: 系統必須在註冊時，若電子郵件已存在則明確回傳「此電子郵件已被使用」訊息（註冊情境優先使用者體驗）
- **FR-009-3**: 系統不得提供獨立的電子郵件存在性檢查API端點（避免帳號枚舉攻擊）

- **FR-010**: 系統必須允許使用者使用有效的 Refresh Token 換取新的 JWT

- **FR-010-1**: 系統必須提供 Token 驗證端點 (`GET /api/auth/validate`) 供其他微服務驗證 JWT 有效性和使用者驗證狀態
- **FR-010-2**: Token 驗證端點必須透過 Bearer Token (Authorization header) 接收要驗證的 JWT
- **FR-010-3**: Token 驗證端點必須在 Token 有效時回傳 200 OK 與完整資訊：isValid, userId, email, username, emailVerified, phoneNumberVerified, expiresAt
- **FR-010-4**: Token 驗證端點返回的驗證狀態（emailVerified, phoneNumberVerified）必須是從資料庫查詢的最新狀態，而非從 JWT claims 讀取
- **FR-010-5**: 其他微服務應使用 Token 驗證端點返回的驗證狀態來決定使用者是否有權限執行特定業務操作
- **FR-010-6**: Token 驗證端點必須在 Token 無效或過期時回傳 401 Unauthorized
- **FR-010-7**: 系統必須在使用者完成驗證後，下次使用 Refresh Token 更新 JWT 時，在新的 JWT claims 中更新驗證狀態
- **FR-010-8**: 建議其他微服務根據業務需求定義驗證要求，例如：競標需要雙重驗證、查看拍賣品不需驗證、發起拍賣需要電子郵件驗證等

- **FR-011**: 系統必須拒絕已過期或被撤銷的 Refresh Token<!--

- **FR-012**: 系統必須允許已登入使用者查詢自己的完整個人資料  ACTION REQUIRED: Define measurable success criteria.

- **FR-013**: 系統必須允許已登入使用者查詢其他使用者的公開資料（僅使用者名稱與建立時間）  These must be technology-agnostic and measurable.

- **FR-014**: 系統必須允許使用者更新自己的使用者名稱與電子郵件-->

- **FR-015**: 系統必須在更新電子郵件時驗證新電子郵件的唯一性

- **FR-016**: 系統必須允許使用者變更密碼，需驗證舊密碼正確性
- **FR-016-1**: 系統必須在使用者變更密碼時，立即撤銷該使用者所有現有的 Refresh Token（強制所有裝置重新登入）
- **FR-017**: 系統必須記錄每個使用者的建立時間與最後更新時間

- **FR-018**: 系統必須確保使用者只能修改自己的資料，不可修改其他使用者的資料

- **FR-019**: 系統必須支援使用者透過電子郵件或手機號碼進行密碼重設
- **FR-019-1**: 系統必須要求使用者在註冊時提供電子郵件和手機號碼（兩者皆為必填）
- **FR-019-2**: 系統必須驗證手機號碼符合國際格式標準（E.164）
- **FR-019-3**: 系統必須在密碼重設請求時，發送 6 位數驗證碼到使用者選擇的管道（email 或 SMS）
- **FR-019-4**: 驗證碼必須有效期限為5分鐘，儲存在 Redis 並使用 TTL 自動清除
- **FR-019-5**: 系統必須限制驗證碼錯誤嘗試次數為 3 次，超過後鎖定該驗證碼
- **FR-019-6**: 系統必須實作驗證碼發送冷卻機制（60 秒內不可重複發送）
- **FR-019-7**: 系統必須在密碼重設成功後，立即撤銷該使用者所有現有的 Refresh Token
- **FR-019-8**: 系統不得洩漏電子郵件或手機號碼是否存在於系統中（安全考量，統一回傳成功訊息）

### 關鍵實體 *(包含資料相關功能)*

- **使用者 (User)**: 代表系統的會員，包含雪花ID（64-bit Long，主鍵）、電子郵件（唯一）、bcrypt雜湊後的密碼（結合雪花ID）、使用者名稱、手機號碼（唯一）、手機驗證狀態、建立時間、更新時間
- **Refresh Token**: 代表使用者的長期身份驗證權杖，包含權杖字串、所屬使用者識別碼（雪花ID）、過期時間、是否已撤銷狀態
- **密碼重設權杖 (PasswordResetToken)**: 代表密碼重設的臨時驗證碼，包含使用者ID、6位數驗證碼、發送方式（Email/SMS）、過期時間（5分鐘）、使用狀態、錯誤嘗試次數，儲存在 Redis

## 成功指標 *(必填)*

### 可衡量成果

- **SC-001**: 使用者能在 1 分鐘內完成註冊與登入流程（從開始填寫到取得存取權杖）
- **SC-002**: 密碼儲存符合業界安全標準（使用強式加密演算法，無法反向解密）
- **SC-003**: JWT 驗證處理時間低於 50 毫秒
- **SC-004**: 系統能同時處理 1000 位使用者的註冊與登入請求而不降級
- **SC-005**: 登入成功率達 99%（排除使用者輸入錯誤的情況）
- **SC-006**: 電子郵件唯一性驗證準確率 100%（無重複註冊發生）
- **SC-007**: 使用者資料更新成功率達 99%
- **SC-008**: 權杖更新請求回應時間低於 100 毫秒

## 假設

- 假設系統採用通用驗證機制（VerificationToken），可支援多種驗證場景（註冊、更新信箱、更新手機等）
- 假設通知服務整合優先使用雲端方案（AWS SES + AWS SNS），備用方案為 SMTP + Twilio
- 假設 JWT 使用 HS256 對稱金鑰演算法簽章，所有微服務共享相同密鑰
- 假設 JWT 密鑰透過環境變數或密鑰管理服務取得，不寫入程式碼或設定檔
- 假設系統使用 bcrypt 演算法（work factor 12）進行密碼雜湊，並與雪花ID組合
- 假設雪花ID生成使用成熟的 .NET 套件（IdGen 或 Snowflake.Core），需配置 Worker ID 與 Datacenter ID
- 假設不需要記錄使用者登入歷史
- 假設 Refresh Token 在使用後不會自動輪換（refresh token rotation）
- 假設驗證碼有效期為5分鐘，長度為6位數字，儲存在 Redis 並使用 TTL 自動清除
- 假設驗證碼錯誤嘗試限制為 3 次
- 假設驗證碼發送冷卻時間為 60 秒
- 假設 PasswordResetToken 功能未來可考慮整合到通用 VerificationToken 機制以簡化架構

## 範圍外

以下功能明確不在本次實作範圍內：

- 多因素驗證（2FA）
- OAuth2 第三方登入（Google、Facebook 等）
- 使用者帳號停用或刪除功能
- 使用者角色與權限管理（管理員、一般使用者等）
- 登入歷史記錄
- 帳號鎖定機制（多次登入失敗後鎖定）
- Refresh Token 自動輪換
- 裝置管理（查看已登入的裝置並登出）
- 驗證碼的圖形驗證碼（CAPTCHA）防護
- 更新電子郵件與手機號碼功能（User Story 4.5，未來功能）

---

## 跨微服務驗證狀態整合指南

### 設計概述

本章節說明如何在微服務架構中整合會員驗證狀態檢查，確保未驗證的使用者無法執行需要驗證的業務操作（例如：競標商品、發起拍賣等）。

**核心設計原則**：
1. **註冊與驗證分離**：註冊立即成功，驗證是獨立的後續步驟
2. **漸進式權限**：未驗證用戶可登入使用基本功能，驗證後解鎖更多權限
3. **JWT + API 雙重機制**：結合 JWT payload 快速檢查和 API 查詢最新狀態
4. **業務邏輯自主**：各微服務根據自身業務需求決定驗證要求

### 驗證狀態獲取方案

#### 方案 1：從 JWT Token 讀取（快速但可能過時）

**適用場景**：非關鍵操作、快速檢查、性能優先

JWT payload (claims) 中包含的驗證狀態欄位：
```json
{
  "sub": "1234567890123456789",
  "email": "user@example.com",
  "username": "張三",
  "emailVerified": true,
  "phoneNumberVerified": false,
  "exp": 1705315200,
  "iat": 1705314300
}
```

**優點**：
- ✅ 無需額外 API 調用
- ✅ 性能最佳
- ✅ 減少 MemberService 負載

**缺點**：
- ❌ 驗證狀態可能過時（JWT 簽發後用戶完成驗證，但 token 未刷新）
- ❌ 不適合關鍵業務操作

**實作方式**（BiddingService / AuctionService）：
```csharp
// 從 JWT claims 讀取驗證狀態
var emailVerified = User.Claims.FirstOrDefault(c => c.Type == "emailVerified")?.Value == "true";
var phoneVerified = User.Claims.FirstOrDefault(c => c.Type == "phoneNumberVerified")?.Value == "true";

if (!emailVerified || !phoneVerified)
{
    return Forbid("此操作需要完成電子郵件和手機號碼驗證");
}
```

#### 方案 2：調用 Token 驗證 API（準確但需額外調用）

**適用場景**：關鍵業務操作、需要最新狀態、安全性優先

**API 端點**：`GET /api/auth/validate`
- Header: `Authorization: Bearer {jwt_token}`

**回應範例**：
```json
{
  "success": true,
  "data": {
    "isValid": true,
    "userId": 1234567890123456789,
    "email": "user@example.com",
    "username": "張三",
    "emailVerified": true,
    "phoneNumberVerified": true,
    "expiresAt": "2026-01-15T11:00:00Z"
  }
}
```

**優點**：
- ✅ 獲取最新的驗證狀態（從資料庫查詢）
- ✅ 準確可靠
- ✅ 一次調用獲取完整使用者資訊

**缺點**：
- ❌ 需要額外的 HTTP 調用
- ❌ 增加延遲
- ❌ 增加 MemberService 負載

**實作方式**（BiddingService / AuctionService）：
```csharp
// 調用 MemberService 的 validate API
public async Task<TokenValidationResult> ValidateTokenAsync(string jwtToken)
{
    var client = _httpClientFactory.CreateClient("MemberService");
    client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", jwtToken);
    
    var response = await client.GetAsync("/api/auth/validate");
    
    if (!response.IsSuccessStatusCode)
    {
        return new TokenValidationResult { IsValid = false };
    }
    
    var result = await response.Content.ReadFromJsonAsync<TokenValidationResponse>();
    return result.Data;
}

// 在業務邏輯中使用
var validation = await _authService.ValidateTokenAsync(jwtToken);

if (!validation.EmailVerified || !validation.PhoneNumberVerified)
{
    return Forbid("此操作需要完成電子郵件和手機號碼驗證");
}
```

#### 方案 3：混合方案（推薦）

**適用場景**：平衡性能和準確性

**實作策略**：
1. 首先從 JWT claims 讀取驗證狀態（快速檢查）
2. 如果 JWT 顯示未驗證，則調用 validate API 確認最新狀態
3. 如果 JWT 顯示已驗證，則信任該狀態（因為驗證是單向的，不會從 true 變回 false）

```csharp
// 可選的輔助方法（BiddingService / AuctionService）
public async Task<bool> IsFullyVerifiedAsync(ClaimsPrincipal user, string jwtToken)
{
    // 步驟 1: 從 JWT 快速檢查
    var emailVerifiedClaim = user.FindFirst("emailVerified")?.Value == "true";
    var phoneVerifiedClaim = user.FindFirst("phoneNumberVerified")?.Value == "true";
    
    // 如果 JWT 顯示已完成驗證，直接信任
    if (emailVerifiedClaim && phoneVerifiedClaim)
    {
        return true;
    }
    
    // 步驟 2: 如果未驗證，調用 API 確認最新狀態
    // （可能用戶剛完成驗證，但 JWT 還未刷新）
    var validation = await _authService.ValidateTokenAsync(jwtToken);
    
    return validation.EmailVerified && validation.PhoneNumberVerified;
}
```

### 各微服務驗證需求建議

#### BiddingService（競標服務）

| 操作 | 驗證需求 | 理由 |
|------|---------|------|
| 查看競標列表 | 無需驗證 | 公開資訊 |
| 參與競標（金額 < 10,000） | 需電子郵件驗證 | 確保聯絡方式有效 |
| 參與競標（金額 >= 10,000） | **需雙重驗證** | 高價值交易需更高信任度 |
| 查看自己的競標歷史 | 無需驗證 | 自己的資料 |

**實作範例**：
```csharp
[HttpPost("bids")]
[Authorize]
public async Task<IActionResult> PlaceBid([FromBody] PlaceBidRequest request)
{
    var jwtToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
    var validation = await _authService.ValidateTokenAsync(jwtToken);
    
    // 高額競標需要雙重驗證
    if (request.Amount >= 10000)
    {
        if (!validation.EmailVerified || !validation.PhoneNumberVerified)
        {
            return Forbid(new
            {
                code = "VERIFICATION_REQUIRED",
                message = "高額競標需要完成電子郵件和手機號碼驗證",
                requiredVerifications = new
                {
                    emailVerified = validation.EmailVerified,
                    phoneNumberVerified = validation.PhoneNumberVerified
                }
            });
        }
    }
    // 低額競標至少需要電子郵件驗證
    else if (!validation.EmailVerified)
    {
        return Forbid(new
        {
            code = "EMAIL_VERIFICATION_REQUIRED",
            message = "競標需要先完成電子郵件驗證"
        });
    }
    
    // 繼續競標邏輯...
    return Ok(await _biddingService.PlaceBidAsync(request));
}
```

#### AuctionService（拍賣服務）

| 操作 | 驗證需求 | 理由 |
|------|---------|------|
| 瀏覽拍賣品 | 無需驗證 | 公開資訊 |
| 查看拍賣品詳情 | 無需驗證 | 公開資訊 |
| 發起拍賣 | **需雙重驗證** | 確保賣家身份真實可信 |
| 管理自己的拍賣 | 需電子郵件驗證 | 確保聯絡方式有效 |

### 驗證狀態更新時機

#### JWT Token 中的驗證狀態

| 時機 | 驗證狀態來源 | 說明 |
|------|------------|------|
| **登入時** | 資料庫當前狀態 | 簽發 JWT 時從 User 表讀取 |
| **Token 有效期內** | JWT claims（固定） | 不變，即使用戶完成驗證 |
| **使用 Refresh Token 更新** | 資料庫最新狀態 | 重新簽發 JWT，更新驗證狀態 |

#### Validate API 返回的驗證狀態

| 時機 | 驗證狀態來源 | 說明 |
|------|------------|------|
| **每次調用** | 資料庫即時查詢 | 始終返回最新狀態 |

### 錯誤碼設計

建議統一使用以下錯誤碼：

| 錯誤碼 | HTTP 狀態 | 說明 | 解決方案 |
|--------|----------|------|---------|
| `EMAIL_VERIFICATION_REQUIRED` | 403 | 需要電子郵件驗證 | 引導用戶前往驗證 |
| `PHONE_VERIFICATION_REQUIRED` | 403 | 需要手機號碼驗證 | 引導用戶前往驗證 |
| `VERIFICATION_REQUIRED` | 403 | 需要雙重驗證 | 引導用戶完成所有驗證 |
| `INVALID_TOKEN` | 401 | Token 無效 | 重新登入 |
| `TOKEN_EXPIRED` | 401 | Token 過期 | 使用 Refresh Token 更新 |

**錯誤回應範例**：
```json
{
  "success": false,
  "error": {
    "code": "VERIFICATION_REQUIRED",
    "message": "此操作需要完成電子郵件和手機號碼驗證",
    "requiredVerifications": {
      "emailVerified": true,
      "phoneNumberVerified": false
    },
    "verificationUrl": "/account/verify"
  }
}
```

### 實作檢查清單

**MemberService（會員服務）**：
- [ ] JWT 生成時在 payload 中包含 `emailVerified` 和 `phoneNumberVerified`
- [ ] 實作 `/api/auth/validate` 端點
- [ ] Validate 端點從資料庫查詢最新驗證狀態（不從 JWT claims 讀取）
- [ ] 使用 Refresh Token 更新 JWT 時，重新查詢並更新驗證狀態
- [ ] 提供驗證端點供用戶完成驗證

**BiddingService / AuctionService**：
- [ ] 配置 MemberService 的 HTTP Client
- [ ] 實作 Token 驗證服務包裝類
- [ ] 在關鍵操作前檢查驗證狀態
- [ ] 返回清晰的錯誤訊息，告知用戶需要完成哪些驗證
- [ ] （可選）實作混合檢查機制優化性能
- [ ] 定義各操作的驗證需求矩陣

### 注意事項

**安全性考量**：
1. 不要過度依賴 JWT claims：關鍵操作應調用 validate API 確認最新狀態
2. 錯誤訊息要清晰：告知用戶需要完成哪些驗證，並提供驗證入口連結
3. 日誌記錄：記錄驗證檢查失敗的情況，便於分析和監控
4. 緩存策略：可考慮短時間緩存 validate API 結果（例如 30 秒），減少重複調用

**用戶體驗考量**：
1. 漸進式引導：在用戶嘗試需驗證的操作時，引導其完成驗證
2. 明確的權限邊界：清楚告知哪些功能需要驗證
3. 驗證獎勵機制：可考慮給已驗證用戶更多權益（例如：更高的競標限額）
