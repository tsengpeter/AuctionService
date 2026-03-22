# Requirements Checklist: Backend Modular Monolith Scaffold

**Feature**: 001-backend-scaffold  
**Status**: Draft

---

## Functional Requirements

- [x] **FR-001** - 系統提供統一的 API 入口點，所有模組端點透過同一服務對外暴露
- [x] **FR-002** - 五個模組（Member、Auction、Bidding、Ordering、Notification）的資料儲存各自隔離，互不直接存取
- [x] **FR-003** - 系統提供 API 互動文件（Swagger），列出所有端點、請求/回應結構
- [x] **FR-004** - 所有 API 回應遵循統一結構 `{ success, data?, error?, statusCode }`
- [x] **FR-005** - 系統支援 JWT 身份驗證，保護端點在未授權時回傳 401
- [ ] **FR-006** - 模組間透過非同步事件機制通訊，發布者不直接相依訂閱者
- [x] **FR-007** - 系統對進入端點的請求資料進行驗證，失敗時回傳 422 含欄位錯誤說明
- [x] **FR-008** - 系統提供健康檢查端點，回報服務與資料庫連線狀態
- [x] **FR-009** - 系統提供 Docker Compose 設定，一鍵啟動開發所需外部依賴
- [x] **FR-010** - 所有模組支援單元測試（無外部依賴）與整合測試（隔離臨時資料庫）

---

## Success Criteria Verification

- [ ] **SC-001** - 從 clone 到 API 文件可存取時間 < 10 分鐘
- [x] **SC-002** - `dotnet build` 零錯誤，所有模組可獨立編譯
- [x] **SC-003** - `dotnet test` 全綠，初始骨架測試通過率 100%
- [x] **SC-004** - 所有 API 端點回應結構一致率 100%
- [x] **SC-005** - 修改任一模組業務邏輯，不需修改其他模組任何檔案
- [x] **SC-006** - 健康檢查端點回應時間 < 100ms

---

## Clarifications Needed

- [x] JWT Secret 最短長度確認（建議 32 字元）
- [x] API 文件路徑確認（如 `/swagger`）
- [x] 健康檢查端點路徑確認（如 `/health`）
- [x] Docker Compose 資料庫 port 設定確認
