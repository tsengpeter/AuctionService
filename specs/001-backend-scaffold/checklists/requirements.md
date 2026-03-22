# Requirements Checklist: Backend Modular Monolith Scaffold

**Feature**: 001-backend-scaffold
**Status**: Draft

---

## Functional Requirements

- [x] **FR-001** - 系統提供統一的 API 入口點，將各模組端點透過路由聚合統一對外暴露
- [x] **FR-002** - 五個模組（Member、Auction、Bidding、Ordering、Notification）的資料存取各自隔離，彼此不直接依賴
- [x] **FR-003** - 系統提供 API 互動文件（Swagger），可查閱所有端點、請求與回應結構
- [x] **FR-004** - 所有 API 回應皆遵循統一結構 `{ success, data?, error?, statusCode }`
- [x] **FR-005** - 系統支援 JWT 身份驗證，保護端點在未授權時回傳 401
- [x] **FR-006** - 模組間透過非同步事件機制通訊，發布者不直接相依訂閱者
- [x] **FR-007** - 系統對進入端點的請求皆進行驗證，失敗時回傳 422 含欄位錯誤說明
- [x] **FR-008** - 系統提供健康檢查端點，包含各模組資料庫的健康狀態
- [x] **FR-009** - 系統提供 Docker Compose 設定，可快速啟動所有外部依賴
- [x] **FR-010** - 每個模組支援單元測試且無需外部依賴，整合測試使用隔離的資料庫
- [x] **FR-011** - 系統提供 `Dockerfile`，可將應用程式打包為 Docker Image，採多階段建置（multi-stage build）以最小化 Image 大小；執行階段不含 .NET SDK；容器以非 root 使用者執行；所有設定透過環境變數注入

---

## Success Criteria Verification

- [x] **SC-001** - 從 clone 到 API 文件可查閱約需不到 10 分鐘
- [x] **SC-002** - `dotnet build` 無錯誤，所有模組可獨立編譯
- [x] **SC-003** - `dotnet test` 通過，初始骨架測試通過率 100%
- [x] **SC-004** - 所有 API 端點回應結構一致性 100%
- [x] **SC-005** - 修改任何模組業務邏輯，不需修改其他模組任何檔案
- [x] **SC-006** - 健康檢查端點回應時間 < 100ms
- [x] **SC-007** - `docker build -t auctionservice:latest .` 成功，`docker run` 後 `GET /health` 回傳 Healthy

---

## Clarifications Needed

- [x] JWT Secret 最小長度確認（建議 32 字元）
- [x] API 文件路徑確定（為 `/swagger`）
- [x] 健康檢查端點路徑確定（為 `/health`）
- [x] Docker Compose 資料庫 port 設定確定