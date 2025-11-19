# 多 Service 並行開發：代理上下文管理策略

## 問題說明

當多個 service 分支（例如 `001-member-service`, `002-product-service`, `003-auction-service`）同時開發時，如果都更新全域的 `.github/copilot-instructions.md`，合併回 master 時會產生衝突。

## 解決策略：Feature-Specific 代理上下文

### 1. Feature 分支階段（開發期間）

**每個 feature 使用自己的代理上下文檔案**：
```
specs/001-member-service/.copilot-context.md
specs/002-product-service/.copilot-context.md
specs/003-auction-service/.copilot-context.md
```

**優點**：
- ✅ 避免合併衝突
- ✅ 每個 feature 有專屬的技術上下文
- ✅ 可獨立更新，互不影響
- ✅ 檔案位置與 feature 規格文件在同一目錄，易於管理

### 2. Master 分支階段（合併後）

當 feature 分支合併到 master 後，執行統一更新腳本，將所有 feature 的代理上下文整合到全域檔案：

```bash
# 在 master 分支執行
.specify/scripts/bash/merge-agent-contexts.sh
```

**整合後的全域檔案結構**：
```markdown
# AuctionService Development Guidelines

## Active Technologies

### Member Service (001-member-service)
- ASP.NET Core 9, C# 12
- PostgreSQL 16

### Product Service (002-product-service)
- ASP.NET Core 9, C# 12
- PostgreSQL 16

### Auction Service (003-auction-service)
- ASP.NET Core 9, C# 12
- PostgreSQL 16 + Redis

## Commands by Service

### Member Service
cd src/MemberService && dotnet test

### Product Service
cd src/ProductService && dotnet test

...
```

## 工作流程

### 開發新 Feature（在 Feature 分支）

```bash
# 1. 建立並切換到 feature 分支
git checkout -b 002-product-service

# 2. 執行 /speckit.plan 產生規格文件
# （會自動建立 specs/002-product-service/.copilot-context.md）

# 3. 開發過程中，Copilot 會讀取 feature-specific 的上下文

# 4. 提交時，包含 feature-specific 上下文檔案
git add specs/002-product-service/
git commit -m "feat: add product service specification"
```

### 合併到 Master

```bash
# 1. 切換到 master 並合併 feature 分支
git checkout master
git merge 002-product-service

# 2. 執行整合腳本（更新全域代理上下文）
.specify/scripts/bash/merge-agent-contexts.sh

# 3. 提交整合後的全域檔案
git add .github/copilot-instructions.md
git commit -m "chore: update global agent context after merging 002-product-service"
```

## 如何讓 Copilot 讀取 Feature-Specific 上下文？

### 方法 1：手動在工作區設定（推薦）

在 VS Code 的 `.vscode/settings.json` 添加：

```json
{
  "github.copilot.advanced": {
    "additionalContext": [
      "specs/001-member-service/.copilot-context.md"
    ]
  }
}
```

**注意**：`.vscode/settings.json` 應加入 `.gitignore`，避免影響其他開發者。

### 方法 2：使用 Workspace Copilot Instructions (VS Code 2024+)

如果您使用的 VS Code 版本支援 workspace-level Copilot instructions，可以在專案根目錄建立：

```
.github/copilot-instructions.md  # 全域（master 分支）
specs/001-member-service/.copilot-context.md  # Feature-specific（feature 分支）
```

Copilot 會自動合併兩者的上下文（feature-specific 優先）。

### 方法 3：修改 update-agent-context.sh 腳本

修改腳本行為，使其在 feature 分支時產生 feature-specific 檔案：

```bash
# 偵測當前分支
CURRENT_BRANCH=$(git branch --show-current)

# 如果在 feature 分支，產生 feature-specific 檔案
if [[ "$CURRENT_BRANCH" != "master" && "$CURRENT_BRANCH" != "main" ]]; then
    COPILOT_FILE="$SPEC_DIR/.copilot-context.md"
else
    COPILOT_FILE="$REPO_ROOT/.github/copilot-instructions.md"
fi
```

## 目前實作狀態

✅ **已完成**：
- 建立 `specs/001-member-service/.copilot-context.md`（feature-specific 上下文）
- 移除 `.github/copilot-instructions.md`（避免合併衝突）

⏳ **待完成**：
- 建立 `.specify/scripts/bash/merge-agent-contexts.sh`（整合腳本）
- 修改 `update-agent-context.sh` 支援 feature-specific 模式
- 更新 `.gitignore` 排除 `.vscode/settings.json`

## 最佳實踐

1. **Feature 分支開發期間**：僅更新 `specs/{feature}/.copilot-context.md`
2. **Commit 前檢查**：確保沒有誤改全域 `.github/copilot-instructions.md`
3. **PR Review**：檢查是否只包含 feature-specific 變更
4. **合併後整合**：在 master 分支執行 `merge-agent-contexts.sh`
5. **定期同步**：每次合併 feature 後，更新全域上下文

## 替代方案（不推薦）

### 方案 A：使用 Git Merge Driver

設定 `.gitattributes`：
```
.github/copilot-instructions.md merge=union
```

**缺點**：會累積重複內容，需手動清理。

### 方案 B：在 Master 分支才更新全域檔案

Feature 分支完全不建立代理上下文。

**缺點**：開發期間缺少 AI 輔助，降低效率。

---

**結論**：Feature-specific 代理上下文策略在保持開發效率的同時，避免了合併衝突，是多 service 並行開發的最佳實踐。
