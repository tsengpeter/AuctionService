using FluentValidation.Results;

namespace AuctionService.Core.Exceptions;

/// <summary>
/// 驗證異常
/// </summary>
public class ValidationException : Exception
{
    /// <summary>
    /// 驗證錯誤清單
    /// </summary>
    public IEnumerable<ValidationFailure> Errors { get; }

    /// <summary>
    /// 建構子
    /// </summary>
    /// <param name="errors">驗證錯誤清單</param>
    public ValidationException(IEnumerable<ValidationFailure> errors)
        : base("Validation failed")
    {
        Errors = errors;
    }

    /// <summary>
    /// 建構子
    /// </summary>
    /// <param name="message">錯誤訊息</param>
    public ValidationException(string message)
        : base(message)
    {
        Errors = new List<ValidationFailure>();
    }
}