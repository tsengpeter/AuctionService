namespace AuctionService.Core.DTOs.Common;

/// <summary>
/// 本地化回應資訊
/// </summary>
public class LocalizedResponseInfo
{
    /// <summary>
    /// 狀態代碼
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 狀態名稱
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 本地化訊息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 分類
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// 嚴重性
    /// </summary>
    public string Severity { get; set; } = string.Empty;
}