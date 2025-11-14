# 功能規格：會員服務# Feature Specification: [FEATURE NAME]



**功能分支**: `001-member-service`  **Feature Branch**: `[###-feature-name]`  

**建立日期**: 2025-11-04  **Created**: [DATE]  

**狀態**: 草稿  **Status**: Draft  

**輸入**: 使用者描述："實作會員服務，負責使用者註冊、登入、身份驗證與個人資料管理"**Input**: User description: "$ARGUMENTS"



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

## User Scenarios & Testing *(mandatory)*



## 使用者情境與測試 *(必填)*<!--

  IMPORTANT: User stories should be PRIORITIZED as user journeys ordered by importance.

### 使用者故事 1 - 新使用者註冊與登入 (優先順序: P1)  Each user story/journey must be INDEPENDENTLY TESTABLE - meaning if you implement just ONE of them,

  you should still have a viable MVP (Minimum Viable Product) that delivers value.

新使用者透過提供電子郵件、密碼與使用者名稱完成註冊，註冊成功後自動登入系統，取得存取權杖以使用其他服務功能。  

  Assign priorities (P1, P2, P3, etc.) to each story, where P1 is the most critical.

**優先順序理由**: 這是會員服務的核心價值，沒有註冊與登入功能，其他所有功能都無法使用。這是最小可行產品（MVP）的基礎。  Think of each story as a standalone slice of functionality that can be:

  - Developed independently

**獨立測試**: 可透過提供有效的註冊資訊（電子郵件、密碼、使用者名稱）完整測試註冊流程，驗證使用者能成功建立帳號並自動登入，提供立即可用的身份驗證功能。  - Tested independently

  - Deployed independently

**驗收情境**:  - Demonstrated to users independently

-->

1. **Given** 使用者尚未註冊, **When** 使用者提供有效的電子郵件、密碼（8 字元以上）與使用者名稱進行註冊, **Then** 系統建立新帳號並自動登入，回傳 JWT 存取權杖

2. **Given** 電子郵件已被註冊, **When** 使用者嘗試使用相同電子郵件註冊, **Then** 系統拒絕註冊並顯示「此電子郵件已被使用」錯誤訊息### User Story 1 - [Brief Title] (Priority: P1)

3. **Given** 使用者提供的密碼少於 8 字元, **When** 使用者嘗試註冊, **Then** 系統拒絕註冊並顯示「密碼必須至少 8 個字元」錯誤訊息

4. **Given** 使用者提供無效的電子郵件格式, **When** 使用者嘗試註冊, **Then** 系統拒絕註冊並顯示「請提供有效的電子郵件地址」錯誤訊息[Describe this user journey in plain language]

5. **Given** 使用者已註冊, **When** 使用者使用正確的電子郵件與密碼登入, **Then** 系統驗證成功並回傳 JWT 存取權杖與 Refresh Token

6. **Given** 使用者已註冊, **When** 使用者使用錯誤的密碼登入, **Then** 系統拒絕登入並顯示「電子郵件或密碼錯誤」訊息**Why this priority**: [Explain the value and why it has this priority level]



---**Independent Test**: [Describe how this can be tested independently - e.g., "Can be fully tested by [specific action] and delivers [specific value]"]



### 使用者故事 2 - 權杖更新 (優先順序: P2)**Acceptance Scenarios**:



使用者的 JWT 存取權杖過期後，可使用 Refresh Token 換取新的 JWT，無需重新輸入密碼，提供流暢的使用體驗。1. **Given** [initial state], **When** [action], **Then** [expected outcome]

2. **Given** [initial state], **When** [action], **Then** [expected outcome]

**優先順序理由**: JWT 有效期限僅 15 分鐘，若無權杖更新機制，使用者需頻繁重新登入，嚴重影響使用體驗。此功能是基本身份驗證流程的重要補充。

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



**優先順序理由**: 資料更新與密碼變更是帳號管理的輔助功能，雖然重要但非系統初期運作的必要條件。### Edge Cases



**獨立測試**: 可透過已登入的使用者更新資料欄位，驗證系統正確更新並驗證資料唯一性；透過密碼變更流程驗證舊密碼驗證與新密碼設定。<!--

  ACTION REQUIRED: The content in this section represents placeholders.

**驗收情境**:  Fill them out with the right edge cases.

-->

1. **Given** 使用者已登入, **When** 使用者更新使用者名稱, **Then** 系統成功更新並回傳更新後的資料

2. **Given** 使用者已登入, **When** 使用者更新電子郵件為未被使用的新電子郵件, **Then** 系統成功更新- What happens when [boundary condition]?

3. **Given** 使用者已登入, **When** 使用者更新電子郵件為已被其他使用者使用的電子郵件, **Then** 系統拒絕更新並顯示「此電子郵件已被使用」訊息- How does system handle [error scenario]?

4. **Given** 使用者已登入, **When** 使用者提供正確的舊密碼與新密碼（8 字元以上）進行密碼變更, **Then** 系統成功更新密碼

5. **Given** 使用者已登入, **When** 使用者提供錯誤的舊密碼進行密碼變更, **Then** 系統拒絕變更並顯示「舊密碼錯誤」訊息## Requirements *(mandatory)*

6. **Given** 使用者已登入, **When** 使用者提供的新密碼少於 8 字元, **Then** 系統拒絕變更並顯示「密碼必須至少 8 個字元」訊息

<!--

---  ACTION REQUIRED: The content in this section represents placeholders.

  Fill them out with the right functional requirements.

### 邊界情況

- 當短時間內有大量註冊請求（潛在攻擊），系統如何保護？
- 當使用者忘記密碼時，如何重設密碼？（目前規格未涵蓋此功能）
- 當電子郵件服務無法使用時，註冊流程如何處理？（如需發送驗證信）
- 註冊功能仍可被用於枚舉已註冊的電子郵件（安全性與使用者體驗的取捨，優先使用者體驗）
- 登入功能不洩漏帳號存在資訊（統一回傳模糊錯誤訊息）

## 需求 *(必填)*



### 功能需求*Example of marking unclear requirements:*



### 功能需求*Example of marking unclear requirements:*

- **FR-000**: 系統必須使用雪花ID (Snowflake ID, 64-bit Long) 作為使用者的主鍵
- **FR-000-1**: 系統必須使用成熟的 .NET 雪花ID套件 (如 IdGen 或 Snowflake.Core) 生成唯一識別碼
- **FR-000-2**: 雪花ID生成器必須配置 Worker ID 與 Datacenter ID 以確保分散式環境下的唯一性

- **FR-001**: 系統必須允許使用者提供電子郵件、密碼與使用者名稱進行註冊- **FR-006**: System MUST authenticate users via [NEEDS CLARIFICATION: auth method not specified - email/password, SSO, OAuth?]

- **FR-002**: 系統必須驗證電子郵件格式符合標準格式（包含 @ 符號與有效網域）- **FR-007**: System MUST retain user data for [NEEDS CLARIFICATION: retention period not specified]

- **FR-003**: 系統必須驗證電子郵件的唯一性，不允許重複註冊
- **FR-003-1**: 系統必須驗證使用者名稱僅包含字母（英文、繁體中文等語言字元）和空格，不允許數字、底線、連字號等特殊字元
- **FR-003-2**: 系統必須驗證使用者名稱長度為 3-50 字元（中文字元計為 1 字元）
- **FR-004**: 系統必須驗證密碼長度至少 8 個字元### Key Entities *(include if feature involves data)*

- **FR-005**: 系統必須將密碼加密儲存，不可儲存明文密碼
- **FR-005-1**: 系統必須使用 bcrypt 演算法進行密碼雜湊
- **FR-005-2**: 系統必須將密碼與使用者的雪花ID組合後進行雜湊: bcrypt(password + snowflakeId)，提供額外的安全保護
- **FR-005-3**: bcrypt 的 work factor (成本因子) 建議設定為 12（可根據安全需求調整）

- **FR-006**: 系統必須在註冊成功後自動讓使用者登入，回傳 JWT 與 Refresh Token- **[Entity 1]**: [What it represents, key attributes without implementation]

- **FR-007**: 系統必須允許使用者使用電子郵件與密碼進行登入- **[Entity 2]**: [What it represents, relationships to other entities]

- **FR-008**: 系統必須在登入成功後回傳 JWT 存取權杖（有效期限 15 分鐘）與 Refresh Token（有效期限 7 天）
- **FR-008-1**: JWT 必須使用 HS256 對稱金鑰演算法 (HMAC-SHA256) 進行簽章
- **FR-008-2**: JWT 密鑰必須透過環境變數或密鑰管理服務取得，不可寫入程式碼或設定檔

- **FR-009**: 系統必須在登入失敗時顯示明確的錯誤訊息（但不洩漏帳號是否存在的資訊）
- **FR-009-1**: 系統必須在登入失敗時（無論是電子郵件不存在或密碼錯誤）統一回傳「電子郵件或密碼錯誤」訊息
- **FR-009-2**: 系統必須在註冊時，若電子郵件已存在則明確回傳「此電子郵件已被使用」訊息（註冊情境優先使用者體驗）
- **FR-009-3**: 系統不得提供獨立的電子郵件存在性檢查API端點（避免帳號枚舉攻擊）## Success Criteria *(mandatory)*

- **FR-010**: 系統必須允許使用者使用有效的 Refresh Token 換取新的 JWT

- **FR-011**: 系統必須拒絕已過期或被撤銷的 Refresh Token<!--

- **FR-012**: 系統必須允許已登入使用者查詢自己的完整個人資料  ACTION REQUIRED: Define measurable success criteria.

- **FR-013**: 系統必須允許已登入使用者查詢其他使用者的公開資料（僅使用者名稱與建立時間）  These must be technology-agnostic and measurable.

- **FR-014**: 系統必須允許使用者更新自己的使用者名稱與電子郵件-->

- **FR-015**: 系統必須在更新電子郵件時驗證新電子郵件的唯一性

- **FR-016**: 系統必須允許使用者變更密碼，需驗證舊密碼正確性
- **FR-016-1**: 系統必須在使用者變更密碼時，立即撤銷該使用者所有現有的 Refresh Token（強制所有裝置重新登入）
- **FR-017**: 系統必須記錄每個使用者的建立時間與最後更新時間

- **FR-018**: 系統必須確保使用者只能修改自己的資料，不可修改其他使用者的資料- **SC-001**: [Measurable metric, e.g., "Users can complete account creation in under 2 minutes"]

- **SC-002**: [Measurable metric, e.g., "System handles 1000 concurrent users without degradation"]

### 關鍵實體 *(包含資料相關功能)*- **SC-003**: [User satisfaction metric, e.g., "90% of users successfully complete primary task on first attempt"]

- **SC-004**: [Business metric, e.g., "Reduce support tickets related to [X] by 50%"]

- **使用者 (User)**: 代表系統的會員，包含雪花ID（64-bit Long，主鍵）、電子郵件（唯一）、bcrypt雜湊後的密碼（結合雪花ID）、使用者名稱、建立時間、更新時間
- **更新權杖 (RefreshToken)**: 代表使用者的長期身份驗證權杖，包含權杖字串、所屬使用者識別碼（雪花ID）、過期時間、是否已撤銷狀態

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

- 假設系統初期不需要電子郵件驗證功能（使用者註冊後即可使用，不需驗證電子郵件）
- 假設不需要實作密碼重設功能（忘記密碼）
- 假設不需要實作多因素驗證（2FA）
- 假設 JWT 使用 HS256 對稱金鑰演算法簽章，所有微服務共享相同密鑰
- 假設 JWT 密鑰透過環境變數或密鑰管理服務取得，不寫入程式碼或設定檔
- 假設系統使用 bcrypt 演算法（work factor 12）進行密碼雜湊，並與雪花ID組合
- 假設雪花ID生成使用成熟的 .NET 套件（IdGen 或 Snowflake.Core），需配置 Worker ID 與 Datacenter ID
- 假設不需要記錄使用者登入歷史
- 假設 Refresh Token 在使用後不會自動輪換（refresh token rotation）

## 範圍外

以下功能明確不在本次實作範圍內：

- 電子郵件驗證（註冊後需驗證電子郵件）
- 忘記密碼與密碼重設功能
- 多因素驗證（2FA）
- OAuth2 第三方登入（Google、Facebook 等）
- 使用者帳號停用或刪除功能
- 使用者角色與權限管理（管理員、一般使用者等）
- 登入歷史記錄
- 帳號鎖定機制（多次登入失敗後鎖定）
- Refresh Token 自動輪換
- 裝置管理（查看已登入的裝置並登出）
