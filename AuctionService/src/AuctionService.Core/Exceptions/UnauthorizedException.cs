namespace AuctionService.Core.Exceptions;

/// <summary>
/// 未授權異常
/// </summary>
public class UnauthorizedException : Exception
{
    /// <summary>
    /// 建構子
    /// </summary>
    /// <param name="message">錯誤訊息</param>
    public UnauthorizedException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// 建構子
    /// </summary>
    public UnauthorizedException()
        : base("Unauthorized access")
    {
    }
}