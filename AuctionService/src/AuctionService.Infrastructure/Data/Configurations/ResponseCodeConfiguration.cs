using AuctionService.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuctionService.Infrastructure.Data.Configurations;

/// <summary>
/// ResponseCode 實體配置
/// </summary>
public class ResponseCodeConfiguration : IEntityTypeConfiguration<ResponseCode>
{
    public void Configure(EntityTypeBuilder<ResponseCode> builder)
    {
        builder.ToTable("ResponseCodes");

        builder.HasKey(rc => rc.Code);

        builder.Property(rc => rc.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(rc => rc.MessageZhTw)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(rc => rc.MessageEn)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(rc => rc.Category)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(rc => rc.Severity)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(rc => rc.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        // 種子資料
        builder.HasData(
            // 成功回應
            new ResponseCode
            {
                Code = "AUCTION_CREATED",
                Name = "商品已建立",
                MessageZhTw = "商品建立成功",
                MessageEn = "Auction created successfully",
                Category = "Success",
                Severity = "Info",
                CreatedAt = DateTime.UtcNow
            },
            new ResponseCode
            {
                Code = "AUCTION_UPDATED",
                Name = "商品已更新",
                MessageZhTw = "商品更新成功",
                MessageEn = "Auction updated successfully",
                Category = "Success",
                Severity = "Info",
                CreatedAt = DateTime.UtcNow
            },
            new ResponseCode
            {
                Code = "AUCTION_DELETED",
                Name = "商品已刪除",
                MessageZhTw = "商品刪除成功",
                MessageEn = "Auction deleted successfully",
                Category = "Success",
                Severity = "Info",
                CreatedAt = DateTime.UtcNow
            },

            // 客戶端錯誤
            new ResponseCode
            {
                Code = "AUCTION_NOT_FOUND",
                Name = "商品不存在",
                MessageZhTw = "查無此商品",
                MessageEn = "Auction not found",
                Category = "ClientError",
                Severity = "Warning",
                CreatedAt = DateTime.UtcNow
            },
            new ResponseCode
            {
                Code = "AUCTION_INVALID_END_TIME",
                Name = "結束時間無效",
                MessageZhTw = "結束時間必須至少在 1 小時之後",
                MessageEn = "End time must be at least 1 hour from now",
                Category = "ClientError",
                Severity = "Warning",
                CreatedAt = DateTime.UtcNow
            },
            new ResponseCode
            {
                Code = "AUCTION_HAS_BIDS",
                Name = "商品已有出價",
                MessageZhTw = "已有出價的商品無法編輯或刪除",
                MessageEn = "Cannot edit or delete auction with existing bids",
                Category = "ClientError",
                Severity = "Warning",
                CreatedAt = DateTime.UtcNow
            },
            new ResponseCode
            {
                Code = "AUCTION_UNAUTHORIZED",
                Name = "無權限操作",
                MessageZhTw = "無權限操作此商品",
                MessageEn = "Unauthorized to access this auction",
                Category = "ClientError",
                Severity = "Error",
                CreatedAt = DateTime.UtcNow
            },
            new ResponseCode
            {
                Code = "FOLLOW_ALREADY_EXISTS",
                Name = "已追蹤",
                MessageZhTw = "已在追蹤清單中",
                MessageEn = "Already following this auction",
                Category = "ClientError",
                Severity = "Info",
                CreatedAt = DateTime.UtcNow
            },
            new ResponseCode
            {
                Code = "FOLLOW_OWN_AUCTION",
                Name = "無法追蹤自己的商品",
                MessageZhTw = "無法追蹤自己的商品",
                MessageEn = "Cannot follow your own auction",
                Category = "ClientError",
                Severity = "Warning",
                CreatedAt = DateTime.UtcNow
            },
            new ResponseCode
            {
                Code = "FOLLOW_LIMIT_EXCEEDED",
                Name = "追蹤數量超過限制",
                MessageZhTw = "追蹤商品數量已達上限 (500)",
                MessageEn = "Follow limit exceeded (500)",
                Category = "ClientError",
                Severity = "Warning",
                CreatedAt = DateTime.UtcNow
            },

            // 伺服器錯誤
            new ResponseCode
            {
                Code = "INTERNAL_SERVER_ERROR",
                Name = "內部伺服器錯誤",
                MessageZhTw = "伺服器發生錯誤，請稍後再試",
                MessageEn = "Internal server error, please try again later",
                Category = "ServerError",
                Severity = "Error",
                CreatedAt = DateTime.UtcNow
            },
            new ResponseCode
            {
                Code = "BIDDING_SERVICE_UNAVAILABLE",
                Name = "競標服務無法使用",
                MessageZhTw = "競標服務暫時無法使用",
                MessageEn = "Bidding service is unavailable",
                Category = "ServerError",
                Severity = "Error",
                CreatedAt = DateTime.UtcNow
            }
        );
    }
}