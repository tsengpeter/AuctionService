using AuctionService.Core.DTOs.Common;
using FluentValidation;

namespace AuctionService.Core.Validators;

/// <summary>
/// AuctionQueryParameters 驗證器
/// </summary>
public class AuctionQueryParametersValidator : AbstractValidator<AuctionQueryParameters>
{
    public AuctionQueryParametersValidator()
    {
        // 頁碼驗證
        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage("頁碼必須大於 0");

        // 每頁筆數驗證 (檢查原始輸入值)
        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .WithMessage("每頁筆數必須大於 0");

        // 搜尋關鍵字驗證
        RuleFor(x => x.SearchTerm)
            .MaximumLength(100)
            .WithMessage("搜尋關鍵字長度不能超過 100 個字元")
            .When(x => !string.IsNullOrWhiteSpace(x.SearchTerm));

        // 分類 ID 驗證
        RuleFor(x => x.CategoryId)
            .GreaterThan(0)
            .WithMessage("分類 ID 必須大於 0")
            .When(x => x.CategoryId.HasValue);

        // 價格範圍驗證
        RuleFor(x => x.MinPrice)
            .GreaterThanOrEqualTo(0)
            .WithMessage("最低價格不能小於 0")
            .When(x => x.MinPrice.HasValue);

        RuleFor(x => x.MaxPrice)
            .GreaterThanOrEqualTo(0)
            .WithMessage("最高價格不能小於 0")
            .When(x => x.MaxPrice.HasValue);

        RuleFor(x => x)
            .Must(x => !x.MinPrice.HasValue || !x.MaxPrice.HasValue || x.MinPrice.Value <= x.MaxPrice.Value)
            .WithMessage("最低價格不能大於最高價格")
            .When(x => x.MinPrice.HasValue && x.MaxPrice.HasValue);
    }
}