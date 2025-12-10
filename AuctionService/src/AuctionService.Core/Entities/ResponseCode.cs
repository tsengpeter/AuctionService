namespace AuctionService.Core.Entities;

/// <summary>
/// API 回應代碼實體
/// </summary>
public class ResponseCode
{
    /// <summary>
    /// 狀態代碼
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 顯示名稱
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 繁體中文訊息
    /// </summary>
    public string MessageZhTw { get; set; } = string.Empty;

    /// <summary>
    /// 英文訊息
    /// </summary>
    public string MessageEn { get; set; } = string.Empty;

    /// <summary>
    /// 分類
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// 嚴重性
    /// </summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// 建立時間
    /// </summary>
    public DateTime CreatedAt { get; set; }
}