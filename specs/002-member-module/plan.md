# Implementation Plan: Member 模組 — 註冊、驗證與個人資料

**Branch**: `002-member-module` | **Date**: 2026-04-08 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `/specs/002-member-module/spec.md`

## Summary

實作 Member 模組的完整認證與個人資料管理功能：使用者以 email + username + password 註冊（BCrypt 雜湊）、以 email + password 登入取得 JWT HS256 Access Token（15 分鐘）與 Refresh Token（7 天，Token Rotation）、登出、查詢與更新個人資料（結構化地址：country/city/postal_code/address_line）、密碼變更（成功後撤銷所有 Refresh Token）。IP 速率限制（登入端點每分鐘 5 次失敗上限），Refresh Token 每日背景清理。

## Technical Context

**Language/Version**: C# 13 / .NET 10  
**Primary Dependencies**: MediatR 12.x、FluentValidation 12.x、EF Core 10 + Npgsql、BCrypt.Net-Next、Microsoft.AspNetCore.Authentication.JwtBearer 10  
**Storage**: PostgreSQL 16，schema: `member`，tables: `users`、`refresh_tokens`  
**Testing**: xUnit 2.9 + Testcontainers.PostgreSql 4.x + FluentAssertions 8.x + NSubstitute 5.x  
**Target Platform**: Linux server（Docker）  
**Project Type**: Modular Monolith Web API  
**Performance Goals**: API p95 ≤ 200ms（BCrypt 驗證除外，預計 300–400ms）  
**Constraints**: JWT Secret ≥ 32 字元，來自環境變數；密碼明文不可儲存或回傳  
**Scale/Scope**: 初期 ≤ 10,000 使用者，單節點部署

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| 原則 | 狀態 | 說明 |
|------|------|------|
| I. Code Quality First | ✅ PASS | 私有建構子 + 靜態工廠、介面隔離（IPasswordHasher / IJwtTokenService）、Domain 與 Infrastructure 分離 |
| II. TDD（NON-NEGOTIABLE） | ✅ PASS | 所有 Handler 先寫單元測試（NSubstitute mock interfaces），再寫實作；整合測試用 Testcontainers |
| III. UX Consistency | ✅ PASS | 所有回應使用 `ApiResponse<T>` 包裝；驗證錯誤 422 含 field/message；401 不洩漏帳密哪個錯誤 |
| IV. Performance | ✅ PASS | 登入因 BCrypt 預計 300–400ms（p95），已在 SC-002 接受「3 秒內完成」；其餘端點目標 ≤200ms |
| V. Observability | ✅ PASS | 結構化 logging 於 Handler 層記錄關鍵操作（登入失敗、token 撤銷）；使用 Microsoft.Extensions.Logging |
| Documentation Language (zh-TW) | ✅ PASS | plan.md / data-model.md / quickstart.md 均以繁體中文撰寫 |

**結論**: 無 Constitution 違規，所有 Gate 通過，可進行實作。

## Project Structure

### Documentation (this feature)

```text
specs/002-member-module/
├── plan.md          ✅ 本檔案
├── research.md      ✅ Phase 0 完成
├── data-model.md    ✅ Phase 1 完成
├── quickstart.md    ✅ Phase 1 完成
├── contracts/
│   └── openapi.yaml ✅ Phase 1 完成
└── tasks.md         ⏳ Phase 2（/speckit.tasks）
```

### Source Code (repository root)

```text
src/Modules/Member/
├── Member.csproj
├── Domain/
│   ├── MemberUser.cs
│   ├── RefreshToken.cs
│   └── MemberRole.cs
├── Application/
│   ├── Abstractions/
│   │   ├── IPasswordHasher.cs
│   │   └── IJwtTokenService.cs
│   ├── Commands/
│   │   ├── Register/
│   │   ├── Login/
│   │   ├── RefreshToken/
│   │   ├── Logout/
│   │   ├── UpdateProfile/
│   │   └── ChangePassword/
│   └── Queries/
│       └── GetMe/
└── Infrastructure/
    ├── Persistence/
    │   ├── MemberDbContext.cs
    │   ├── Configurations/
    │   └── Migrations/
    ├── Services/
    │   ├── BcryptPasswordHasher.cs
    │   └── JwtTokenService.cs
    ├── BackgroundServices/
    │   └── RefreshTokenCleanupService.cs
    └── DependencyInjection.cs

src/AuctionService.Api/Controllers/
├── AuthController.cs    # POST /api/auth/{register,login,refresh,logout}
└── UsersController.cs   # GET/PUT /api/users/me, PUT /api/users/me/password

tests/AuctionService.UnitTests/Member/
├── Domain/
├── Application/

tests/AuctionService.IntegrationTests/Member/
```

**Structure Decision**: Modular Monolith — Member 模組獨立 C# 專案，與其他模組（Auction/Bidding）無 EF 跨模組依賴；Controller 位於 API 層，Handler 位於 Application 層。
