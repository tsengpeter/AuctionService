namespace AuctionService.Core.Exceptions;

/// <summary>
/// 商品未找到異常
/// </summary>
public class AuctionNotFoundException : Exception
{
    /// <summary>
    /// 商品 ID
    /// </summary>
    public Guid AuctionId { get; }

    /// <summary>
    /// 建構子
    /// </summary>
    /// <param name="auctionId">商品 ID</param>
    public AuctionNotFoundException(Guid auctionId)
        : base($"Auction with ID {auctionId} was not found")
    {
        AuctionId = auctionId;
    }

    /// <summary>
    /// 建構子
    /// </summary>
    /// <param name="auctionId">商品 ID</param>
    /// <param name="message">錯誤訊息</param>
    public AuctionNotFoundException(Guid auctionId, string message)
        : base(message)
    {
        AuctionId = auctionId;
    }
}