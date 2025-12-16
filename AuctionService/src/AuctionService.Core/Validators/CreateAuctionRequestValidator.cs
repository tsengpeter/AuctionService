using AuctionService.Core.DTOs.Requests;
using FluentValidation;
using System.Text.RegularExpressions;

namespace AuctionService.Core.Validators;

/// <summary>
/// CreateAuctionRequest 驗證器
/// </summary>
public class CreateAuctionRequestValidator : AbstractValidator<CreateAuctionRequest>
{
    public CreateAuctionRequestValidator()
    {
        // 商品名稱驗證 - XSS 防護
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("商品名稱不能為空")
            .Length(3, 200)
            .WithMessage("商品名稱長度必須在 3-200 個字元之間")
            .Must(NotContainHtmlTags)
            .WithMessage("商品名稱不能包含 HTML 標籤");

        // 商品描述驗證 - XSS 防護
        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("商品描述不能為空")
            .MaximumLength(2000)
            .WithMessage("商品描述長度不能超過 2000 個字元")
            .Must(NotContainScriptTags)
            .WithMessage("商品描述不能包含腳本標籤");

        // 起標價驗證
        RuleFor(x => x.StartingPrice)
            .GreaterThan(0)
            .WithMessage("起標價必須大於 0")
            .LessThanOrEqualTo(999999.99m)
            .WithMessage("起標價不能超過 999,999.99");

        // 分類 ID 驗證
        RuleFor(x => x.CategoryId)
            .GreaterThan(0)
            .WithMessage("分類 ID 必須大於 0");

        // 結束時間驗證
        RuleFor(x => x.EndTime)
            .NotEmpty()
            .WithMessage("拍賣結束時間不能為空");

        // 開始時間驗證 (如果有提供)
        // Note: 允許過去的開始時間，因為拍賣可能已經開始
        RuleFor(x => x.StartTime)
            .Must((request, startTime) =>
            {
                if (!startTime.HasValue)
                    return true; // 如果沒有提供開始時間，則有效

                return startTime.Value < request.EndTime;
            })
            .WithMessage("拍賣開始時間必須早於結束時間");
    }

    /// <summary>
    /// 檢查字串是否不包含 HTML 標籤
    /// </summary>
    private bool NotContainHtmlTags(string input)
    {
        if (string.IsNullOrEmpty(input))
            return true;

        // 檢查常見的 HTML 標籤
        var htmlTagPattern = @"<[^>]+>";
        return !Regex.IsMatch(input, htmlTagPattern, RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// 檢查字串是否不包含腳本標籤
    /// </summary>
    private bool NotContainScriptTags(string input)
    {
        if (string.IsNullOrEmpty(input))
            return true;

        // 檢查腳本標籤和事件處理器
        var scriptPatterns = new[]
        {
            @"<script[^>]*>.*?</script>",
            @"<[^>]*\bon\w+\s*=",
            @"javascript:",
            @"vbscript:",
            @"onload\s*=",
            @"onerror\s*=",
            @"onclick\s*=",
            @"onmouseover\s*="
        };

        foreach (var pattern in scriptPatterns)
        {
            if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline))
                return false;
        }

        return true;
    }
}