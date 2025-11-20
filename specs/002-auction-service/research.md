# Research Document: Auction Service

**Branch**: `002-auction-service`  
**Date**: 2025-11-20  
**Purpose**: Technical decision documentation and clarification resolution

---

## Overview

Auction Service 負責商品拍賣的核心業務邏輯，包含商品的建立、查詢、管理與使用者追蹤功能。本文件記錄關鍵技術決策，解決規格中的 NEEDS CLARIFICATION 項目，並提供實作方向建議。

## Technical Stack Summary

- **Framework**: ASP.NET Core 9 Web API, C# 12
- **Database**: PostgreSQL 16 (Code-First with EF Core 9)
- **Architecture**: Clean Architecture (4-layer: Domain/Application/Infrastructure/API)
- **API Gateway**: YARP (not Ocelot)
- **ID Generation**: Snowflake ID (IdGen 3.x package)
- **Caching**: Redis (IDistributedCache)
- **Object Storage**: AWS S3 / Azure Blob Storage / MinIO
- **Full-Text Search**: PostgreSQL tsvector + GIN Index
- **Concurrency Control**: Optimistic Locking (EF Core RowVersion)
- **No AutoMapper**: Manual POCO mapping for DTOs
- **No Minimal APIs**: Controller-based design

---

## Decision 1: Snowflake ID for Primary Key

### Context
商品主鍵需要唯一識別碼，與 Member Service 的 User ID 保持一致的生成策略。

### Decision
使用 **Snowflake ID (64-bit Long)** 作為 Auction 實體的主鍵。

### Rationale
- **分散式友善**: 無需中央協調，每個服務實例可獨立生成唯一 ID
- **時間有序**: ID 包含時間戳記，天然按時間排序，有利於索引效能
- **空間效率**: 8 bytes (Long) 相比 GUID 的 16 bytes 節省 50% 儲存空間
- **與 Member Service 一致**: 使用相同策略便於跨服務整合與維護

### Implementation
```csharp
// Domain Layer: MemberService.Domain/Interfaces/ISnowflakeIdGenerator.cs
public interface ISnowflakeIdGenerator
{
    long GenerateId();
}

// Infrastructure Layer: AuctionService.Infrastructure/IdGeneration/SnowflakeIdGenerator.cs
using IdGen;

public class SnowflakeIdGenerator : ISnowflakeIdGenerator
{
    private readonly IdGenerator _generator;
    
    public SnowflakeIdGenerator(IConfiguration configuration)
    {
        var workerId = configuration.GetValue<int>("Snowflake:WorkerId");
        var datacenterId = configuration.GetValue<int>("Snowflake:DatacenterId");
        var epoch = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        
        _generator = new IdGenerator(workerId, new IdGeneratorOptions
        {
            IdStructure = new IdStructure(41, 10, 12),
            TimeSource = new DefaultTimeSource(epoch)
        });
    }
    
    public long GenerateId() => _generator.CreateId();
}
```

### Configuration
```json
// appsettings.json
{
  "Snowflake": {
    "WorkerId": 1,       // 每個服務實例唯一 (0-1023)
    "DatacenterId": 0    // 每個資料中心唯一 (0-1023)
  }
}
```

### Package
- **IdGen 3.x**: `dotnet add package IdGen --version 3.0.0` (或最新穩定版)

### Alternatives Considered
- **GUID**: 16 bytes 空間成本高，無時間順序，索引效能較差
- **Auto-increment Integer**: 不適合分散式環境，需中央協調，擴展性差
- **ULID**: 時間有序但 128-bit，空間成本高於 Snowflake ID

---

## Decision 2: Optimistic Locking for Concurrency Control

### Context
多個使用者可能同時編輯同一商品，需要並發控制機制防止資料覆蓋。

### Decision
使用 **Optimistic Locking (樂觀鎖)** 搭配 EF Core 的 `RowVersion` 欄位。

### Rationale
- **低衝突場景**: 商品編輯為低頻操作，樂觀鎖效能最佳（無鎖等待）
- **EF Core 原生支援**: 使用 `[Timestamp]` 屬性自動管理版本號
- **明確錯誤處理**: 捕捉 `DbUpdateConcurrencyException` 返回 409 Conflict
- **使用者體驗**: 前端接收 409 後重新載入最新資料，避免資料遺失

### Implementation
```csharp
// Domain Layer: Auction.cs
public class Auction
{
    public long Id { get; set; }
    // ... other properties
    
    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;  // EF Core 自動管理
}

// Application Layer: AuctionService.cs
public async Task UpdateAuctionAsync(long id, UpdateAuctionRequest request)
{
    try
    {
        var auction = await _repository.GetByIdAsync(id);
        // ... update logic
        await _repository.UpdateAsync(auction);
    }
    catch (DbUpdateConcurrencyException)
    {
        throw new AuctionConcurrentEditException("商品已被其他使用者修改，請重新載入後再試");
    }
}

// API Layer: ExceptionHandlingMiddleware.cs
catch (AuctionConcurrentEditException ex)
{
    return new ProblemDetails
    {
        Status = StatusCodes.Status409Conflict,
        Title = "AUCTION_CONCURRENT_EDIT",
        Detail = ex.Message
    };
}
```

### Database Schema
```sql
-- EF Core Migration 會自動生成
ALTER TABLE Auctions ADD RowVersion bytea NOT NULL DEFAULT '\x00000000';
```

### Alternatives Considered
- **Pessimistic Locking (悲觀鎖)**: 需要鎖等待，效能較差，適合高衝突場景（拍賣服務非此情境）
- **Last-Write-Wins**: 無並發控制，資料可能被覆蓋，不可接受

---

## Decision 3: Redis Cache Fallback for Bidding Service

### Context
Bidding Service 負責出價邏輯，Auction Service 需呼叫其 API 取得最高出價。當 Bidding Service 不可用時，需降級方案確保商品資訊仍可查詢。

### Decision
使用 **Redis Cache Fallback** 策略：優先呼叫 Bidding Service，失敗則返回快取的最後已知出價並標註「可能過時」。

### Rationale
- **高可用性**: 即使 Bidding Service 不可用，商品查詢仍可正常回應
- **資料即時性**: 成功呼叫時即時更新快取，確保快取資料新鮮度
- **明確提示**: 使用快取時明確標註 `dataSource: "cache"`，避免誤導使用者
- **防止級聯失敗**: 設定 Timeout (3 秒) 防止無限等待，快速降級

### Implementation
```csharp
// Application Layer: BiddingServiceClient.cs
public class BiddingServiceClient : IBiddingServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly IDistributedCache _cache;
    private readonly ILogger<BiddingServiceClient> _logger;
    
    public async Task<CurrentBidResponse?> GetCurrentBidAsync(long auctionId)
    {
        var cacheKey = $"bid:{auctionId}";
        
        try
        {
            // 1. 嘗試呼叫 Bidding Service (Timeout: 3 秒)
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            var response = await _httpClient.GetAsync($"/api/bids/{auctionId}/current", cts.Token);
            
            if (response.IsSuccessStatusCode)
            {
                var bid = await response.Content.ReadFromJsonAsync<CurrentBidResponse>();
                
                // 2. 更新快取 (TTL: 5 分鐘)
                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(bid), 
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });
                
                _logger.LogInformation("Successfully fetched current bid for auction {AuctionId}", auctionId);
                return bid with { DataSource = "live" };
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Bidding Service timeout for auction {AuctionId}", auctionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call Bidding Service for auction {AuctionId}", auctionId);
        }
        
        // 3. 降級: 從快取讀取最後已知出價
        var cachedBid = await _cache.GetStringAsync(cacheKey);
        if (cachedBid != null)
        {
            var bid = JsonSerializer.Deserialize<CurrentBidResponse>(cachedBid);
            _logger.LogWarning("Returning cached bid for auction {AuctionId}", auctionId);
            return bid with { DataSource = "cache" };
        }
        
        // 4. 快取未命中: 返回 null
        _logger.LogWarning("No cached bid available for auction {AuctionId}", auctionId);
        return null;
    }
}

// Response DTO
public record CurrentBidResponse(
    decimal Amount,
    long BidderId,
    DateTime BidTime,
    string DataSource  // "live" or "cache"
);
```

### API Response Example
```json
{
  "data": {
    "auctionId": 123,
    "currentBid": {
      "amount": 1500.00,
      "bidderId": 456,
      "bidTime": "2025-11-20T10:30:00Z",
      "dataSource": "cache"
    }
  },
  "metadata": {
    "statusCode": "SUCCESS",
    "statusName": "Success",
    "message": "出價資訊可能過時，Bidding Service 暫時無法連線"
  }
}
```

### Configuration
```json
// appsettings.json
{
  "BiddingService": {
    "BaseUrl": "https://bidding-service.example.com",
    "Timeout": "00:00:03"
  },
  "Redis": {
    "ConnectionString": "localhost:6379",
    "InstanceName": "AuctionService:"
  }
}
```

### Alternatives Considered
- **同步呼叫無降級**: Bidding Service 不可用時直接失敗，使用者體驗差
- **Circuit Breaker**: 複雜度高，快取降級已足夠應對短暫失敗

---

## Decision 4: PostgreSQL Full-Text Search

### Context
使用者需要搜尋商品名稱與描述，初期使用 PostgreSQL 內建全文檢索，保留未來遷移至 Elasticsearch 的彈性。

### Decision
使用 **PostgreSQL tsvector + GIN Index** 實作全文檢索。

### Rationale
- **無額外依賴**: 無需引入 Elasticsearch，降低系統複雜度與成本
- **效能足夠**: GIN 索引查詢效能優異，適合初期商品數量（<100,000）
- **中文支援**: 使用 PostgreSQL 內建 simple 分詞器或 zhparser 擴充套件
- **未來遷移**: 若需進階功能（fuzzy search、同義詞）可無痛遷移至 Elasticsearch

### Implementation

#### Database Schema
```sql
-- Migration: Add SearchVector column
ALTER TABLE Auctions ADD COLUMN SearchVector tsvector;

-- Create GIN Index
CREATE INDEX idx_auction_search ON Auctions USING GIN(SearchVector);

-- Trigger: Auto-update SearchVector when Title or Description changes
CREATE OR REPLACE FUNCTION update_auction_search_vector()
RETURNS TRIGGER AS $$
BEGIN
  NEW.SearchVector := 
    setweight(to_tsvector('simple', coalesce(NEW.Title, '')), 'A') ||
    setweight(to_tsvector('simple', coalesce(NEW.Description, '')), 'B');
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_update_auction_search
BEFORE INSERT OR UPDATE OF Title, Description ON Auctions
FOR EACH ROW EXECUTE FUNCTION update_auction_search_vector();
```

#### EF Core Implementation
```csharp
// Infrastructure Layer: AuctionRepository.cs
public async Task<List<Auction>> SearchAsync(string keyword, int pageIndex, int pageSize)
{
    var query = _context.Auctions
        .Where(a => EF.Functions.ToTsVector("simple", a.Title + " " + a.Description)
            .Matches(EF.Functions.PlainToTsQuery("simple", keyword)))
        .OrderByDescending(a => EF.Functions.TsRank(a.SearchVector, 
            EF.Functions.PlainToTsQuery("simple", keyword)))
        .Skip(pageIndex * pageSize)
        .Take(pageSize);
    
    return await query.ToListAsync();
}
```

#### Alternative: Using Raw SQL
```csharp
public async Task<List<Auction>> SearchAsync(string keyword, int pageIndex, int pageSize)
{
    var sql = @"
        SELECT * FROM Auctions
        WHERE SearchVector @@ plainto_tsquery('simple', {0})
        ORDER BY ts_rank(SearchVector, plainto_tsquery('simple', {0})) DESC
        OFFSET {1} LIMIT {2}";
    
    return await _context.Auctions
        .FromSqlRaw(sql, keyword, pageIndex * pageSize, pageSize)
        .ToListAsync();
}
```

### Chinese Text Search
```sql
-- 選項 1: 使用 zhparser 擴充套件（需安裝）
CREATE TEXT SEARCH CONFIGURATION chinese (PARSER = zhparser);
ALTER TEXT SEARCH CONFIGURATION chinese ADD MAPPING FOR n,v,a,i,e,l WITH simple;

-- 選項 2: 使用 simple 分詞器（內建，不支援中文分詞）
-- 適合初期，關鍵字精確比對
```

### Alternatives Considered
- **LIKE 查詢**: 效能差，無法處理大量資料，無相關性排序
- **Elasticsearch**: 初期過度設計，增加維護成本，可作為後續升級選項

---

## Decision 5: Cloud Object Storage for Images

### Context
商品圖片需要儲存與訪問，資料庫儲存 BLOB 效能差且成本高。

### Decision
使用 **雲端物件儲存 (AWS S3 / Azure Blob Storage / MinIO)** 儲存圖片，資料庫僅存 URL。

### Rationale
- **效能**: 透過 CDN 加速全球訪問，降低延遲
- **成本**: 物件儲存比資料庫 BLOB 儲存便宜數倍
- **擴展性**: 無需擔心資料庫儲存空間限制
- **靈活性**: MinIO 提供 S3 相容 API，初期可自架降低成本，後續無痛遷移至 AWS/Azure

### Implementation

#### AWS S3 Example
```csharp
// Infrastructure Layer: S3ImageStorageService.cs
using Amazon.S3;
using Amazon.S3.Transfer;

public class S3ImageStorageService : IImageStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    
    public S3ImageStorageService(IAmazonS3 s3Client, IConfiguration config)
    {
        _s3Client = s3Client;
        _bucketName = config["AWS:S3:BucketName"]!;
    }
    
    public async Task<string> UploadImageAsync(long auctionId, IFormFile file)
    {
        // 1. 驗證檔案
        ValidateImage(file);
        
        // 2. 生成唯一檔名
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var key = $"auctions/{auctionId}/{timestamp}_{file.FileName}";
        
        // 3. 上傳至 S3
        using var stream = file.OpenReadStream();
        var uploadRequest = new TransferUtilityUploadRequest
        {
            InputStream = stream,
            Key = key,
            BucketName = _bucketName,
            ContentType = file.ContentType,
            CannedACL = S3CannedACL.PublicRead
        };
        
        var transferUtility = new TransferUtility(_s3Client);
        await transferUtility.UploadAsync(uploadRequest);
        
        // 4. 返回 URL
        return $"https://{_bucketName}.s3.amazonaws.com/{key}";
    }
    
    private void ValidateImage(IFormFile file)
    {
        // 檔案大小驗證 (5MB)
        if (file.Length > 5 * 1024 * 1024)
            throw new InvalidImageException("圖片大小不可超過 5MB");
        
        // 檔案格式驗證
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
            throw new InvalidImageException("僅支援 JPG, PNG, WebP 格式");
        
        // MIME type 驗證
        var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowedMimeTypes.Contains(file.ContentType))
            throw new InvalidImageException("無效的圖片格式");
    }
}
```

#### Database Schema
```csharp
public class Auction
{
    public long Id { get; set; }
    public string Title { get; set; } = null!;
    // ... other properties
    
    // 選項 1: JSON 陣列 (推薦)
    public List<string> ImageUrls { get; set; } = new();
    
    // 選項 2: PostgreSQL TEXT[]
    // public string[] ImageUrls { get; set; } = Array.Empty<string>();
}

// EF Core Configuration
public class AuctionConfiguration : IEntityTypeConfiguration<Auction>
{
    public void Configure(EntityTypeBuilder<Auction> builder)
    {
        // JSON 陣列配置
        builder.Property(a => a.ImageUrls)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null)!)
            .HasColumnType("jsonb");
    }
}
```

#### MinIO Alternative (Self-Hosted)
```json
// appsettings.json
{
  "MinIO": {
    "Endpoint": "localhost:9000",
    "AccessKey": "minioadmin",
    "SecretKey": "minioadmin",
    "BucketName": "auction-images",
    "UseSSL": false
  }
}
```

### Image Upload Flow
```
1. 前端 → API Gateway → Auction Service API (POST /api/auctions/{id}/images)
2. 驗證檔案 (大小、格式、MIME type)
3. 上傳至 S3/Blob/MinIO
4. 將 URL 加入 Auction.ImageUrls
5. 更新資料庫
6. 返回 URL 給前端
```

### Rollback Strategy
```csharp
public async Task<List<string>> UploadImagesAsync(long auctionId, List<IFormFile> files)
{
    var uploadedUrls = new List<string>();
    
    try
    {
        foreach (var file in files)
        {
            var url = await UploadImageAsync(auctionId, file);
            uploadedUrls.Add(url);
        }
        return uploadedUrls;
    }
    catch
    {
        // 回滾: 刪除已上傳的圖片
        foreach (var url in uploadedUrls)
        {
            await DeleteImageAsync(url);
        }
        throw;
    }
}
```

### Packages
- AWS S3: `AWSSDK.S3` (NuGet)
- Azure Blob: `Azure.Storage.Blobs` (NuGet)
- MinIO: `Minio` (NuGet, S3-compatible)

---

## Decision 6: Computed Auction Status

### Context
商品有三種狀態：Pending (未開始)、Active (進行中)、Ended (已結束)。需決定是儲存狀態欄位還是查詢時計算。

### Decision
**不儲存 Status 欄位**，改為查詢時依據 StartTime 與 EndTime 即時計算狀態。

### Rationale
- **100% 準確**: 無延遲，狀態與時間完全同步
- **無背景任務**: 避免定時任務更新狀態的複雜度與效能問題
- **簡化邏輯**: 無需處理大量商品同時結束的批次更新
- **效能影響小**: 現代資料庫時間比較運算效能優異，且可透過索引優化

### Implementation

#### Database Schema
```sql
-- 不需要 Status 欄位，僅需時間欄位
CREATE TABLE Auctions (
    Id BIGINT PRIMARY KEY,
    Title VARCHAR(200) NOT NULL,
    -- ... other columns
    StartTime TIMESTAMPTZ NOT NULL,
    EndTime TIMESTAMPTZ NOT NULL
);

-- 索引優化 (篩選進行中商品)
CREATE INDEX idx_auction_endtime ON Auctions(EndTime);
CREATE INDEX idx_auction_starttime ON Auctions(StartTime);
```

#### EF Core Query
```csharp
// Application Layer: AuctionService.cs
public async Task<List<AuctionListItemDto>> GetActiveAuctionsAsync()
{
    var now = DateTime.UtcNow;
    
    return await _context.Auctions
        .Where(a => a.StartTime <= now && a.EndTime > now)  // 進行中
        .Select(a => new AuctionListItemDto
        {
            Id = a.Id,
            Title = a.Title,
            Status = a.StartTime > now ? "Pending" :
                     a.EndTime <= now ? "Ended" : "Active",  // 計算狀態
            EndTime = a.EndTime
        })
        .ToListAsync();
}
```

#### PostgreSQL Computed Column (Optional)
```sql
-- 若需在 SQL 層使用狀態，可建立 GENERATED ALWAYS 欄位
ALTER TABLE Auctions ADD COLUMN Status VARCHAR(10) 
GENERATED ALWAYS AS (
    CASE 
        WHEN NOW() < StartTime THEN 'Pending'
        WHEN NOW() >= StartTime AND NOW() < EndTime THEN 'Active'
        ELSE 'Ended'
    END
) STORED;
```

### Performance Comparison
| 方案 | 查詢效能 | 更新成本 | 準確性 | 複雜度 |
|------|---------|---------|-------|-------|
| 儲存狀態 + 背景任務 | ⭐⭐⭐⭐⭐ | ⭐⭐ (定時批次更新) | ⭐⭐⭐ (有延遲) | ⭐⭐ (高) |
| 查詢時計算 | ⭐⭐⭐⭐ (時間比較快) | ⭐⭐⭐⭐⭐ (無更新) | ⭐⭐⭐⭐⭐ (即時) | ⭐⭐⭐⭐⭐ (低) |

### Alternatives Considered
- **儲存 Status + 背景任務**: 增加複雜度，狀態更新有延遲，大量商品同時結束時效能問題

---

## Decision 7: ResponseCodes Table for API Responses

### Context
API 需要統一的狀態代碼與訊息管理，支援多語系（繁體中文、英文）。

### Decision
使用 **ResponseCodes 資料表** 儲存所有 API 回應的狀態代碼與訊息。

### Rationale
- **集中管理**: 所有錯誤碼與訊息集中在資料庫，便於維護與更新
- **多語系支援**: MessageZhTw 與 MessageEn 欄位支援雙語系
- **可擴充性**: 新增狀態碼無需修改程式碼，直接在資料庫新增
- **一致性**: 所有 API 回應格式統一，前端易於處理

### Database Schema
```sql
CREATE TABLE ResponseCodes (
    Code VARCHAR(50) PRIMARY KEY,  -- e.g., "AUCTION_NOT_FOUND"
    Name VARCHAR(100) NOT NULL,    -- e.g., "Auction Not Found"
    MessageZhTw TEXT NOT NULL,     -- e.g., "找不到該商品"
    MessageEn TEXT NOT NULL,       -- e.g., "Auction not found"
    Category VARCHAR(20) NOT NULL, -- e.g., "ClientError"
    Severity VARCHAR(20) NOT NULL  -- e.g., "Error"
);

-- 初始資料
INSERT INTO ResponseCodes (Code, Name, MessageZhTw, MessageEn, Category, Severity) VALUES
('SUCCESS', 'Success', '操作成功', 'Operation successful', 'Success', 'Info'),
('AUCTION_NOT_FOUND', 'Auction Not Found', '找不到該商品', 'Auction not found', 'ClientError', 'Error'),
('AUCTION_CONCURRENT_EDIT', 'Concurrent Edit Detected', '商品已被其他使用者修改，請重新載入後再試', 'Auction was modified by another user. Please reload and try again', 'ClientError', 'Warning'),
('BIDDING_SERVICE_UNAVAILABLE', 'Bidding Service Unavailable', '出價服務暫時無法連線，顯示的出價資訊可能過時', 'Bidding service is temporarily unavailable. Bid information may be outdated', 'ServerError', 'Warning'),
('UNAUTHORIZED', 'Unauthorized', '需要登入才能執行此操作', 'Authentication required', 'ClientError', 'Error'),
('FORBIDDEN', 'Forbidden', '您沒有權限執行此操作', 'You do not have permission to perform this action', 'ClientError', 'Error');
```

### Implementation
```csharp
// Domain Layer: ResponseCode.cs
public class ResponseCode
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string MessageZhTw { get; set; } = null!;
    public string MessageEn { get; set; } = null!;
    public string Category { get; set; } = null!;
    public string Severity { get; set; } = null!;
}

// Application Layer: ResponseHelper.cs
public class ResponseHelper
{
    private readonly Dictionary<string, ResponseCode> _codes;
    
    public ResponseHelper(IRepository<ResponseCode> repository)
    {
        _codes = repository.GetAll().ToDictionary(c => c.Code);
    }
    
    public ApiResponse<T> CreateResponse<T>(string code, T? data = default, string? language = "zh-TW")
    {
        var responseCode = _codes[code];
        var message = language == "zh-TW" ? responseCode.MessageZhTw : responseCode.MessageEn;
        
        return new ApiResponse<T>
        {
            Data = data,
            Metadata = new ResponseMetadata
            {
                StatusCode = code,
                StatusName = responseCode.Name,
                Message = message
            }
        };
    }
}

// API Response DTOs
public class ApiResponse<T>
{
    public T? Data { get; set; }
    public ResponseMetadata Metadata { get; set; } = null!;
}

public class ResponseMetadata
{
    public string StatusCode { get; set; } = null!;
    public string StatusName { get; set; } = null!;
    public string Message { get; set; } = null!;
}
```

### API Response Example
```json
{
  "data": {
    "id": 123,
    "title": "iPhone 15 Pro Max",
    "currentBid": {
      "amount": 1500.00,
      "dataSource": "cache"
    }
  },
  "metadata": {
    "statusCode": "BIDDING_SERVICE_UNAVAILABLE",
    "statusName": "Bidding Service Unavailable",
    "message": "出價服務暫時無法連線，顯示的出價資訊可能過時"
  }
}
```

---

## Decision 8: Clean Architecture with 4 Layers

### Context
專案需要明確的架構分層，便於維護與測試。

### Decision
採用 **Clean Architecture** 4-layer 結構：Domain → Application → Infrastructure → API。

### Rationale
- **依賴反轉**: 核心業務邏輯 (Domain) 不依賴外部技術細節
- **可測試性**: 各層可獨立測試，Domain 與 Application 層無需依賴資料庫或外部服務
- **可維護性**: 關注點分離，變更影響範圍小
- **與 Member Service 一致**: 保持專案架構一致性

### Project Structure
```
AuctionService/
├── src/
│   ├── AuctionService.Domain/           # 核心業務邏輯
│   │   ├── Entities/                    # Auction, Category, Follow
│   │   ├── ValueObjects/                # (如有需要)
│   │   ├── Interfaces/                  # ISnowflakeIdGenerator, IRepository
│   │   └── Exceptions/                  # DomainException, AuctionNotFoundException
│   ├── AuctionService.Application/      # 用例與業務流程
│   │   ├── DTOs/                        # Request/Response DTOs
│   │   ├── Services/                    # AuctionService, FollowService
│   │   ├── Validators/                  # FluentValidation validators
│   │   └── Interfaces/                  # IBiddingServiceClient, IImageStorageService
│   ├── AuctionService.Infrastructure/   # 技術實作
│   │   ├── Persistence/                 # EF Core DbContext, Repositories
│   │   ├── IdGeneration/                # SnowflakeIdGenerator
│   │   ├── ExternalServices/            # BiddingServiceClient
│   │   ├── Storage/                     # S3ImageStorageService
│   │   └── Caching/                     # Redis cache services
│   └── AuctionService.API/              # Web API
│       ├── Controllers/                 # AuctionsController, FollowsController
│       ├── Middlewares/                 # ExceptionHandlingMiddleware
│       └── Program.cs                   # DI configuration
└── tests/
    ├── AuctionService.Domain.Tests/
    ├── AuctionService.Application.Tests/
    ├── AuctionService.Infrastructure.Tests/
    └── AuctionService.IntegrationTests/
```

### Dependency Flow
```
API → Infrastructure → Application → Domain
      ↓                    ↓
   External Services    Business Logic
```

---

## Decision 9: Manual POCO Mapping (No AutoMapper)

### Context
需要在 Entity 與 DTO 之間進行資料轉換。

### Decision
**不使用 AutoMapper**，採用 **手動 POCO 映射**。

### Rationale
- **效能**: 無反射與運行時類型檢查，效能最佳
- **明確性**: 映射邏輯清晰可見，易於除錯與維護
- **型別安全**: 編譯時檢查，避免執行時錯誤
- **團隊一致性**: 與 Member Service 保持相同策略

### Implementation Example
```csharp
// Application Layer: AuctionService.cs
public async Task<AuctionDetailDto> GetAuctionDetailAsync(long id)
{
    var auction = await _repository.GetByIdAsync(id);
    if (auction == null)
        throw new AuctionNotFoundException();
    
    // 手動映射
    return new AuctionDetailDto
    {
        Id = auction.Id,
        Title = auction.Title,
        Description = auction.Description,
        StartingPrice = auction.StartingPrice,
        StartTime = auction.StartTime,
        EndTime = auction.EndTime,
        CreatorId = auction.CreatorId,
        CategoryId = auction.CategoryId,
        ImageUrls = auction.ImageUrls,
        Status = ComputeStatus(auction.StartTime, auction.EndTime)
    };
}

private string ComputeStatus(DateTime startTime, DateTime endTime)
{
    var now = DateTime.UtcNow;
    if (now < startTime) return "Pending";
    if (now < endTime) return "Active";
    return "Ended";
}
```

### Extension Method Pattern (Optional)
```csharp
// Application Layer: AuctionExtensions.cs
public static class AuctionExtensions
{
    public static AuctionDetailDto ToDetailDto(this Auction auction)
    {
        return new AuctionDetailDto
        {
            Id = auction.Id,
            Title = auction.Title,
            // ... other mappings
        };
    }
    
    public static AuctionListItemDto ToListItemDto(this Auction auction)
    {
        return new AuctionListItemDto
        {
            Id = auction.Id,
            Title = auction.Title,
            // ... other mappings
        };
    }
}
```

---

## Decision 10: Controller-Based API (No Minimal APIs)

### Context
ASP.NET Core 提供 Minimal APIs 與 Controller-based APIs 兩種選擇。

### Decision
使用 **Controller-based APIs**，不使用 Minimal APIs。

### Rationale
- **結構化**: 控制器提供清晰的組織結構，易於維護大型專案
- **豐富功能**: 內建模型綁定、驗證、過濾器、版本控制等功能
- **團隊一致性**: 與 Member Service 保持相同架構
- **工具支援**: IDE 與 Swagger/OpenAPI 工具對控制器支援更完善

### Implementation Example
```csharp
// API Layer: AuctionsController.cs
[ApiController]
[Route("api/[controller]")]
public class AuctionsController : ControllerBase
{
    private readonly IAuctionService _auctionService;
    
    public AuctionsController(IAuctionService auctionService)
    {
        _auctionService = auctionService;
    }
    
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<AuctionListItemDto>>>> GetAuctions(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] long? categoryId = null,
        [FromQuery] string? keyword = null)
    {
        var auctions = await _auctionService.GetAuctionsAsync(pageIndex, pageSize, categoryId, keyword);
        return Ok(CreateSuccessResponse(auctions));
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<AuctionDetailDto>>> GetAuctionDetail(long id)
    {
        var auction = await _auctionService.GetAuctionDetailAsync(id);
        return Ok(CreateSuccessResponse(auction));
    }
    
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ApiResponse<AuctionDetailDto>>> CreateAuction(CreateAuctionRequest request)
    {
        var auction = await _auctionService.CreateAuctionAsync(request, User.GetUserId());
        return CreatedAtAction(nameof(GetAuctionDetail), new { id = auction.Id }, CreateSuccessResponse(auction));
    }
}
```

---

## Summary

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Primary Key | Snowflake ID (64-bit Long) | 分散式友善、時間有序、空間效率 |
| Concurrency Control | Optimistic Locking (RowVersion) | 低衝突、EF Core 原生支援、效能最佳 |
| Bidding Service Fallback | Redis Cache + Timeout | 高可用性、明確提示、防止級聯失敗 |
| Full-Text Search | PostgreSQL tsvector + GIN | 無額外依賴、效能足夠、保留遷移彈性 |
| Image Storage | Cloud Object Storage (S3/Blob/MinIO) | 效能、成本、擴展性 |
| Auction Status | Computed (Query-time) | 100% 準確、無背景任務、簡化邏輯 |
| API Response | ResponseCodes Table | 集中管理、多語系、可擴充 |
| Architecture | Clean Architecture (4-layer) | 依賴反轉、可測試、可維護 |
| DTO Mapping | Manual POCO Mapping | 效能、明確性、型別安全 |
| API Style | Controller-based | 結構化、豐富功能、團隊一致性 |

---

## Next Steps

1. **Phase 1: Design & Contracts** - 建立 data-model.md, contracts/openapi.yaml, quickstart.md
2. **Phase 2: Implementation Plan** - 完成 plan.md 並進行 Constitution Check
3. **Phase 3: Task Decomposition** - 生成 tasks.md
4. **Phase 4: Implementation** - 執行任務清單

---

**Last Updated**: 2025-11-20  
**Author**: GitHub Copilot (Claude Sonnet 4.5)
