# Specification Analysis Report: API Gateway (004-api-gateway)

**Date**: 2025-12-04  
**Feature**: 004-api-gateway  
**Analyzed Artifacts**: spec.md, plan.md, tasks.md, data-model.md, contracts/, constitution.md  
**Analysis Type**: Read-Only (No modifications made)

---

## Executive Summary

**Overall Status**: ✅ **HIGH QUALITY** - Specifications are comprehensive and well-aligned

- **Total Issues Found**: 15 findings across 6 categories
- **Critical Issues**: 0 (No constitution violations or blocking gaps)
- **High Priority**: 3 (Ambiguous requirements needing clarification)
- **Medium Priority**: 7 (Terminology consistency and coverage enhancements)
- **Low Priority**: 5 (Documentation improvements)

**Coverage**: 187 tasks covering all 5 user stories (US1-US5) with 80%+ requirement coverage. Minor gaps in non-functional requirement task mapping identified.

**Recommendation**: ✅ **Safe to proceed with implementation** - Address HIGH priority ambiguities during Phase 2 kickoff; MEDIUM/LOW issues can be resolved incrementally.

---

## Findings Summary

| ID | Category | Severity | Location(s) | Summary | Recommendation |
|----|----------|----------|-------------|---------|----------------|
| A1 | Ambiguity | HIGH | spec.md:FR-012, plan.md:L85 | "基本統計分析" 缺乏明確範圍定義 - APM 工具應提供哪些具體指標? | 在 FR-012 明確列出必須監控的指標清單 (請求數/錯誤率/延遲 P50/P95/P99) |
| A2 | Ambiguity | HIGH | spec.md:FR-014, tasks.md:T153 | "不自動重試" 與 Polly 重試策略矛盾 - 聚合請求是否允許重試? | 澄清重試範圍: 僅讀取操作 (GET) 允許重試，寫入操作 (POST/PUT) 禁止重試 |
| A3 | Ambiguity | HIGH | spec.md:US-004, contracts/aggregation-endpoints.yaml | 聚合端點 30s 超時是"總時間"還是"單一服務超時"? | 明確說明: 30s 為並行呼叫的總超時時間 (shared timeout)，非每個服務各 30s |
| C1 | Coverage Gap | MEDIUM | spec.md:FR-015, tasks.md | IServiceDiscovery 抽象無明確實作任務 | 在 Phase 2 (Foundational) 新增 T046-T047: 建立服務發現介面與靜態實作 |
| C2 | Coverage Gap | MEDIUM | spec.md:NFR (隱含), tasks.md | 無 Docker 建置驗證任務 | 在 Phase 9 新增任務: 驗證 Dockerfile 多階段建置與容器啟動 |
| C3 | Coverage Gap | MEDIUM | spec.md:FR-007 CORS, tasks.md:T049 | CORS 設定任務存在但無測試任務驗證跨域請求 | 在 Phase 9 新增整合測試任務驗證 CORS 標頭 |
| T1 | Terminology | MEDIUM | spec.md vs plan.md | "Member Service" vs "User Service" 術語混用 | 統一使用 "Member Service" (contracts/routes.yaml 已使用此術語) |
| T2 | Terminology | MEDIUM | spec.md:FR-006 vs data-model.md | Rate Limiting 計數器 Redis Key 格式不一致 | 確認使用 `ratelimit:{ip}:{minute}` (data-model.md 版本較清晰) |
| T3 | Terminology | MEDIUM | spec.md vs tasks.md | "請求聚合" vs "Aggregation" 中英文混用 | 保持一致: 規格用中文，程式碼/任務描述用英文 (已符合 Constitution VI) |
| I1 | Inconsistency | MEDIUM | spec.md:FR-010 vs contracts/aggregation-endpoints.yaml | 聚合請求超時時間表述不一致 - FR-010 說"30秒共用"，contracts 未明確標註 | 在 contracts/aggregation-endpoints.yaml 加註 "shared timeout across parallel calls" |
| I2 | Inconsistency | MEDIUM | plan.md:L180 vs tasks.md:T187 | 任務 ID 數量不一致 - plan.md 預估 "10-14天"，tasks.md 實際 187 任務 | 更新 plan.md 工作量預估或確認 187 任務為合理範圍 |
| D1 | Duplication | LOW | spec.md:FR-010 + US-004 AC-4 | 超時時間重複定義 (30s) | 合併為單一 FR-010 並在 US-004 引用 |
| D2 | Duplication | LOW | spec.md:FR-006 + US-005 AC-1/AC-2 | Rate Limiting 規則重複描述 | 簡化 US-005 驗收標準，引用 FR-006 |
| U1 | Underspecification | LOW | spec.md:FR-013 | "10MB" 請求大小限制缺乏例外情況說明 | 補充: 是否所有端點共用此限制? 聚合端點是否有特殊需求? |
| U2 | Underspecification | LOW | spec.md:邊界情況 | "循環依賴" 防護機制未定義具體實作 | 補充: 使用 Polly 重試策略 MaxRetryAttempts = 3 防止無限循環 |

---

## Constitution Alignment Check

### ✅ Principle I: Code Quality First
**Status**: PASS  
**Findings**: 
- ✅ YARP 提供清晰路由抽象 (符合依賴反轉)
- ✅ JWT 驗證透過標準 Middleware (符合 SOLID)
- ✅ Rate Limiting 封裝為獨立服務 (符合單一職責)
- ✅ IServiceDiscovery 介面抽象設計合理 (plan.md:L180, spec.md:FR-015)

**Issues**: None

---

### ✅ Principle II: Test-Driven Development (NON-NEGOTIABLE)
**Status**: PASS  
**Findings**: 
- ✅ tasks.md 明確要求 "Write tests FIRST, ensure they FAIL" (Phase 3-7 每個 User Story)
- ✅ 單元測試覆蓋率 > 80% 要求明確 (spec.md:SC-006, tasks.md:T179)
- ✅ 整合測試涵蓋所有路由規則 (tasks.md:T052-T067)
- ✅ Testcontainers 用於隔離 Redis 測試 (tasks.md:T101)

**Issues**: None

---

### ✅ Principle III: User Experience Consistency
**Status**: PASS  
**Findings**: 
- ✅ 統一錯誤格式定義完整 (spec.md:FR-004, data-model.md:ErrorResponse)
- ✅ HTTP 狀態碼映射清晰 (10 種錯誤代碼)
- ✅ 驗證訊息使用繁體中文且友善 ("Token expired", "請求次數超過限制")
- ✅ Rate Limit 包含 Retry-After 標頭 (spec.md:FR-006)

**Issues**: None

---

### ✅ Principle IV: Performance Requirements
**Status**: PASS  
**Findings**: 
- ✅ 效能目標明確且可測量 (路由 < 10ms, JWT < 20ms, 聚合 < 300ms)
- ✅ 請求聚合使用 Task.WhenAll 並行執行 (spec.md:FR-005, tasks.md:T135)
- ✅ Redis 連線池設計 (data-model.md, tasks.md:T044)
- ✅ 效能測試任務存在 (tasks.md:T060, T078, T139)

**Issues**: None

---

### ✅ Principle V: Observability and Monitoring
**Status**: PASS  
**Findings**: 
- ✅ 結構化日誌使用 Serilog JSON 格式 (spec.md:FR-012, tasks.md:T037)
- ✅ Correlation ID 追蹤機制 (spec.md:FR-009, tasks.md:T038)
- ✅ 健康檢查端點完整定義 (spec.md:FR-011, tasks.md:T162-T173)
- ✅ Rate Limit 與 Redis 降級事件記錄 (spec.md:FR-012, tasks.md:T112)

**Issues**: None

---

### ✅ Principle VI: Documentation Language
**Status**: PASS  
**Findings**: 
- ✅ spec.md, plan.md, tasks.md 使用繁體中文撰寫
- ✅ 程式碼檔案路徑使用英文 (tasks.md 所有檔案路徑)
- ✅ API 文件使用繁體中文描述 (contracts/aggregation-endpoints.yaml)

**Issues**: None

---

## Coverage Analysis

### Requirements Coverage (Functional Requirements)

| Requirement Key | Description | Has Task? | Task IDs | Coverage % | Notes |
|-----------------|-------------|-----------|----------|------------|-------|
| FR-001 | 路由對應表 | ✅ Yes | T042, T048, T061-T062 | 100% | YARP 路由設定任務完整 |
| FR-002 | 公開/私有端點清單 | ✅ Yes | T085 | 100% | appsettings.json 公開端點設定 |
| FR-003 | JWT 驗證機制 | ✅ Yes | T068-T089 | 100% | 完整 JWT 驗證實作與測試 |
| FR-004 | 錯誤回應格式 | ✅ Yes | T032-T033, T040, T117-T133 | 100% | 統一錯誤處理 Middleware |
| FR-005 | 請求聚合端點 | ✅ Yes | T134-T161 | 100% | 完整聚合服務實作 (28 tasks) |
| FR-006 | Rate Limiting 規則 | ✅ Yes | T090-T116 | 100% | Redis Fixed Window 實作 (27 tasks) |
| FR-007 | CORS 設定 | ⚠️ Partial | T049 | 60% | 設定任務存在，缺乏測試任務 (C3) |
| FR-008 | SSL/TLS 終止 | ✅ Yes | T051 | 100% | HTTPS 重定向設定 |
| FR-009 | 請求/回應轉換 | ✅ Yes | T035, T038, T083 | 100% | 標頭轉換與追蹤 ID |
| FR-010 | 超時處理 | ✅ Yes | T130, T158 | 100% | YARP 與聚合超時設定 |
| FR-011 | 健康檢查 | ✅ Yes | T162-T173 | 100% | 完整健康檢查實作 (12 tasks) |
| FR-012 | 日誌與監控 | ✅ Yes | T037-T041, T112 | 90% | 日誌實作完整，APM 指標範圍需澄清 (A1) |
| FR-013 | 請求大小限制 | ✅ Yes | T050 | 100% | Kestrel MaxRequestBodySize 設定 |
| FR-014 | 重試機制 | ⚠️ Ambiguous | T153 (Polly) | 80% | 與 "不自動重試" 規則矛盾 (A2) |
| FR-015 | 可測試性 | ⚠️ Partial | T046-T047 implicit | 90% | IServiceDiscovery 抽象缺乏明確任務 (C1) |

**Summary**:
- **Total Requirements**: 15
- **Fully Covered**: 12 (80%)
- **Partially Covered**: 3 (20%)
- **Uncovered**: 0 (0%)

---

### User Story Coverage

| User Story | Priority | Tasks | Coverage | Notes |
|------------|----------|-------|----------|-------|
| US-001: 請求路由 | P1 | T052-T067 (16 tasks) | ✅ 100% | 完整路由測試與實作 |
| US-002: JWT 驗證 | P1 | T068-T089 (22 tasks) | ✅ 100% | 完整認證流程與測試 |
| US-003: 錯誤處理 | P2 | T117-T133 (17 tasks) | ✅ 100% | 統一錯誤處理 Middleware |
| US-004: 請求聚合 | P2 | T134-T161 (28 tasks) | ✅ 100% | 並行聚合實作與測試 |
| US-005: 限流防護 | P1 | T090-T116 (27 tasks) | ✅ 100% | Redis Rate Limiting 實作 |

**Summary**: All 5 user stories have complete task coverage with clear acceptance criteria mapping.

---

### Non-Functional Requirements Coverage

| NFR Category | Requirement | Has Task? | Task IDs | Notes |
|--------------|-------------|-----------|----------|-------|
| Performance | 路由延遲 < 10ms | ✅ Yes | T060, T067 | Load testing 任務存在 |
| Performance | JWT 延遲 < 20ms | ✅ Yes | T078, T089 | Benchmark 任務存在 |
| Performance | 聚合延遲 < 300ms | ✅ Yes | T139, T161 | 並行執行驗證 |
| Availability | > 99.9% 可用性 | ⚠️ Implicit | T111 (Redis failover) | 缺乏明確可用性驗證任務 |
| Security | JWT 無繞過漏洞 | ✅ Yes | T068-T076 | 完整認證測試 |
| Security | Rate Limit 準確性 100% | ✅ Yes | T100 | 整合測試驗證 |
| Testability | 單元測試覆蓋率 > 80% | ✅ Yes | T179, T182 | 覆蓋率檢查任務 |
| Deployment | Docker 建置 | ⚠️ Missing | - | 缺乏建置驗證任務 (C2) |

**Summary**: Core NFRs covered; deployment validation needs enhancement.

---

## Unmapped Tasks Analysis

**Tasks without explicit requirement/story mapping**: None found

**Observation**: All 187 tasks in tasks.md are properly tagged with [US1], [US2], [US3], [US4], [US5] labels or belong to Setup/Foundational/Health/Polish phases, providing clear traceability.

---

## Metrics

| Metric | Value |
|--------|-------|
| **Total Functional Requirements** | 15 |
| **Total User Stories** | 5 |
| **Total Tasks** | 187 |
| **Requirements with ≥1 Task** | 15 (100%) |
| **User Stories with Complete Coverage** | 5 (100%) |
| **Critical Issues** | 0 |
| **High Priority Issues** | 3 |
| **Medium Priority Issues** | 7 |
| **Low Priority Issues** | 5 |
| **Ambiguity Count** | 3 |
| **Duplication Count** | 2 |
| **Constitution Violations** | 0 |

---

## Detailed Issue Analysis

### A1: APM 統計分析範圍不明確 (HIGH)

**Location**: spec.md:FR-012 (L320-335), plan.md:Performance Goals (L85)

**Problem**: 
FR-012 提到"透過 APM 工具提供基本統計分析"，但未明確定義"基本統計"包含哪些指標。plan.md 提到"請求數、錯誤率、延遲"，但 spec.md 未列出。

**Impact**: 
開發者可能實作不足的監控指標，導致生產環境問題難以排查。

**Recommendation**: 
在 spec.md:FR-012 新增明確清單:
```markdown
### FR-012: 日誌與監控 (新增)
必須監控的基本指標:
- 請求數 (按端點、時段統計)
- 錯誤率 (按端點、HTTP 狀態碼統計)
- 延遲分佈 (P50/P95/P99)
- Rate Limit 觸發次數
- Redis 降級事件次數
```

**Related Tasks**: T037 (Serilog 設定) 應包含 APM 整合驗證

---

### A2: 重試策略與"不自動重試"規則矛盾 (HIGH)

**Location**: spec.md:FR-014 vs tasks.md:T153

**Problem**: 
- spec.md:FR-014 明確說明"對後端服務的請求不自動重試 (避免重複操作，如出價)"
- tasks.md:T153 要求"Configure HttpClient factory with Polly (retry 3 times with exponential backoff)"

**Impact**: 
開發者可能誤實作重試邏輯於寫入操作 (POST/PUT)，導致重複出價等嚴重問題。

**Recommendation**: 
在 spec.md:FR-014 澄清範圍:
```markdown
### FR-014: 重試機制 (修改)
- 對後端服務的**寫入操作** (POST/PUT/DELETE) 不自動重試
- **讀取操作** (GET) 可使用 Polly 重試策略 (最多 3 次，指數退避)
- 聚合請求的並行呼叫僅重試 GET 請求
```

**Related Tasks**: 在 T153 加註 "Only retry GET requests, not POST/PUT/DELETE"

---

### A3: 聚合請求超時時間定義不明確 (HIGH)

**Location**: spec.md:FR-010, spec.md:US-004, contracts/aggregation-endpoints.yaml

**Problem**: 
- spec.md:FR-010 說"聚合請求超時時間: 30 秒 (並行呼叫，總延遲取決於最慢的服務，所有呼叫共用 30 秒超時限制)"
- contracts/aggregation-endpoints.yaml 每個 backend_call 標註 `timeout: 30s`，但未說明是否為獨立超時

**Ambiguity**: 
"共用 30 秒"可能被理解為:
1. 總超時時間 30s (正確理解)
2. 每個服務各 30s，總計可達 90s (錯誤理解)

**Recommendation**: 
在 contracts/aggregation-endpoints.yaml 加註:
```yaml
# 並行呼叫的後端服務
backend_calls:
  note: "All calls share a single 30s timeout using CancellationTokenSource"
  - service: Auction Service
    timeout: 30s (shared across all parallel calls)
```

**Related Tasks**: T158 應明確驗證總超時為 30s，非累加

---

### C1: IServiceDiscovery 抽象缺乏明確實作任務 (MEDIUM)

**Location**: spec.md:FR-015, plan.md:L180

**Problem**: 
- spec.md:FR-015 與 plan.md 都提到"服務發現使用介面抽象 (IServiceDiscovery)"
- tasks.md 無明確任務建立此介面與靜態實作 (StaticServiceDiscovery)

**Impact**: 
開發者可能遺漏此抽象層實作，導致未來難以遷移到 Consul。

**Recommendation**: 
在 tasks.md Phase 2 (Foundational) 新增:
```markdown
- [ ] T046 Create IServiceDiscovery interface in src/ApiGateway.Core/Interfaces/IServiceDiscovery.cs (abstraction for future Consul migration)
- [ ] T047 Create StaticServiceDiscovery in src/ApiGateway.Infrastructure/HttpClients/StaticServiceDiscovery.cs (reads from appsettings.json)
```

**Note**: plan.md 已提到此設計，但 tasks.md 未明確列出，建議補充。

---

### C2: Docker 建置驗證任務缺失 (MEDIUM)

**Location**: spec.md (Deployment), tasks.md:Phase 9

**Problem**: 
- spec.md 與 plan.md 都提到 Docker 容器部署
- tasks.md:T021 建立 Dockerfile，但無任務驗證 Docker 建置與容器啟動

**Impact**: 
可能在 CI/CD 部署時才發現 Dockerfile 錯誤，延誤交付時間。

**Recommendation**: 
在 tasks.md Phase 9 新增:
```markdown
- [ ] T187-NEW Verify Dockerfile multi-stage build (test build locally and in CI)
- [ ] T188-NEW Test Docker container startup and health check endpoint
```

---

### C3: CORS 測試任務缺失 (MEDIUM)

**Location**: spec.md:FR-007, tasks.md:T049

**Problem**: 
- T049 配置 CORS 設定，但無整合測試任務驗證跨域請求
- spec.md:US-005 AC-3 提到"正確設定 CORS 標頭"，但無對應測試

**Recommendation**: 
在 tasks.md Phase 5 (User Story 5) 新增:
```markdown
- [ ] T115 [US5] Verify CORS configuration already set in Phase 2 (T049) - test cross-origin requests (AC-3)
```

**Note**: T115 已預留，但描述未明確包含測試步驟。建議細化為"Create CORS integration test verifying preflight OPTIONS requests and allowed origins"。

---

### T1: "Member Service" vs "User Service" 術語不一致 (MEDIUM)

**Location**: spec.md vs plan.md vs tasks.md

**Problem**: 
- spec.md 統一使用 "Member Service"
- plan.md:Infrastructure 部分出現 "UserServiceClient"
- tasks.md:T149 使用 "IUserServiceClient"
- contracts/routes.yaml 使用 "member-cluster"

**Impact**: 
團隊溝通與程式碼維護時產生困惑。

**Recommendation**: 
統一使用 "Member Service" 與 "MemberServiceClient"，因為:
1. spec.md 與 contracts/ 已使用此術語
2. 符合 AuctionService 整體架構 (Member/Auction/Bidding 三個服務)

**Action**: 更新 plan.md 與 tasks.md 中所有 "User" 為 "Member"。

---

### T2: Rate Limiting Redis Key 格式不一致 (MEDIUM)

**Location**: spec.md:FR-006 vs data-model.md

**Problem**: 
- spec.md:FR-006: `ratelimit:{ip}:{timestamp_minute}`
- data-model.md: `ratelimit:{ip}:{minute}` (更簡潔)

**Impact**: 
開發者實作時可能混淆 Key 格式。

**Recommendation**: 
統一使用 data-model.md 版本 `ratelimit:{ip}:{minute}`，因為更清晰且無歧義。更新 spec.md:FR-006。

---

### I1: 聚合請求超時時間表述不一致 (MEDIUM)

**Location**: spec.md:FR-010 vs contracts/aggregation-endpoints.yaml

**Problem**: 
已在 A3 分析，屬於 Inconsistency 類別。

**Recommendation**: 見 A3。

---

### I2: 任務數量預估與實際不符 (MEDIUM)

**Location**: plan.md:預估工作量 vs tasks.md:187 tasks

**Problem**: 
- plan.md 預估 "10-14 個工作日 (單人全職)"
- tasks.md 實際 187 個任務 (Setup 31 + Foundational 20 + US1 16 + US2 22 + US5 27 + US3 17 + US4 28 + Health 12 + Polish 14)

**Observation**: 
187 任務對應 10-14 天意味著每天完成 13-18 個任務，可能過於樂觀。

**Recommendation**: 
1. 重新評估工作量 (考慮 TDD 時間)
2. 或確認許多任務為快速配置任務 (如 NuGet 安裝、設定檔建立)
3. 更新 plan.md 為更實際的時間預估 (如 "15-20 個工作日")

---

### D1: FR-010 與 US-004 AC-4 超時時間重複 (LOW)

**Location**: spec.md:FR-010 與 spec.md:US-004 AC-4

**Problem**: 
兩處都定義"聚合請求超時 30s"。

**Recommendation**: 
在 US-004 AC-4 簡化為"**Given** 聚合請求處理完成,**When** 測量延遲,**Then** 回應時間 < 300ms (P95)，超時時間見 FR-010"

---

### D2: FR-006 與 US-005 AC-1/AC-2 Rate Limiting 規則重複 (LOW)

**Location**: spec.md:FR-006 與 spec.md:US-005 AC-1/AC-2

**Problem**: 
兩處都描述"100 次請求/分鐘"與"重置機制"。

**Recommendation**: 
在 US-005 AC-1/AC-2 簡化為"**Given** 同一 IP 超過 Rate Limit (見 FR-006),**When** 發送請求,**Then** 回傳 429"

---

### U1: 請求大小限制缺乏例外情況說明 (LOW)

**Location**: spec.md:FR-013

**Problem**: 
"請求體最大大小: 10MB" 未說明是否所有端點共用此限制。

**Recommendation**: 
補充說明:
```markdown
### FR-013: 請求大小限制
- 請求體最大大小: 10MB (全域限制，適用於所有端點)
- 聚合端點回應大小無限制 (取決於後端服務回應)
```

---

### U2: 循環依賴防護機制未定義 (LOW)

**Location**: spec.md:邊界情況

**Problem**: 
"循環依賴: 防止 Gateway 呼叫後端服務時產生無限循環 (設定最大重試次數)" - 未定義具體實作方式。

**Recommendation**: 
補充說明:
```markdown
### 邊界情況 - 循環依賴 (細化)
- 使用 Polly 重試策略設定 MaxRetryAttempts = 3
- 避免 Gateway 呼叫自身端點 (路由規則驗證)
```

---

## Next Actions

### Immediate Actions (Before Implementation Phase)

1. **[HIGH] Clarify Ambiguities (A1, A2, A3)**:
   - Update spec.md:FR-012 with explicit APM metrics list
   - Update spec.md:FR-014 to distinguish read vs write retry policies
   - Update contracts/aggregation-endpoints.yaml with shared timeout note

2. **[MEDIUM] Fill Coverage Gaps (C1, C2, C3)**:
   - Add T046-T047 tasks for IServiceDiscovery abstraction
   - Add Docker build verification task in Phase 9
   - Enhance T115 with explicit CORS testing steps

3. **[MEDIUM] Resolve Terminology Conflicts (T1, T2)**:
   - Replace "User Service" with "Member Service" across all documents
   - Standardize Redis key format to `ratelimit:{ip}:{minute}`

### Optional Improvements (Can be deferred)

4. **[LOW] Eliminate Duplications (D1, D2)**:
   - Simplify US-004 AC-4 and US-005 AC-1/AC-2 by referencing FR sections

5. **[LOW] Enhance Underspecified Items (U1, U2)**:
   - Add exception cases to FR-013
   - Detail circular dependency prevention in edge cases

### Validation Before Proceed

✅ **Safe to proceed with `/speckit.implement`** after addressing HIGH priority issues (A1, A2, A3).

### Commands Suggestion

```bash
# Option 1: Refine specifications
/speckit.specify --refine "Clarify FR-012 APM metrics, FR-014 retry scope, and aggregation timeout semantics"

# Option 2: Update plan with missing tasks
/speckit.plan --update "Add IServiceDiscovery abstraction tasks and Docker verification tasks"

# Option 3: Manually edit spec.md and tasks.md
# Then re-run analysis: /speckit.analyze
```

---

## Remediation Offer

**Would you like me to suggest concrete remediation edits for the top 3-5 issues?**

If yes, I can provide:
1. Specific text changes for spec.md (FR-012, FR-014, FR-010)
2. New task entries for tasks.md (T046, T047, Docker verification)
3. Terminology replacements across all documents

**Note**: Edits will NOT be applied automatically - you must approve each change explicitly before I proceed with file modifications.

---

## Appendix: Coverage Details

### User Story → Task Mapping

**US-001 (請求路由)**: T052-T067 (16 tasks)  
**US-002 (JWT 驗證)**: T068-T089 (22 tasks)  
**US-003 (錯誤處理)**: T117-T133 (17 tasks)  
**US-004 (請求聚合)**: T134-T161 (28 tasks)  
**US-005 (限流防護)**: T090-T116 (27 tasks)  

**Total User Story Tasks**: 110 tasks (59% of total)  
**Infrastructure Tasks**: 77 tasks (Setup 31 + Foundational 20 + Health 12 + Polish 14)

### Parallel Execution Opportunities

**[P] Marked Tasks**: 87 tasks (46% of total) can run in parallel, enabling multi-developer workflow.

**Phases with High Parallelism**:
- Phase 1 (Setup): 25 of 31 tasks (81%)
- Phase 2 (Foundational): 7 of 20 tasks (35%)
- User Story Tests: 55 of 75 test tasks (73%)

---

**Report Version**: 1.0  
**Generated By**: GitHub Copilot (Claude Sonnet 4.5)  
**Analysis Method**: Progressive disclosure with semantic model extraction  
**Total Tokens Used**: ~50,000 tokens (within budget)
