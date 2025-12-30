using AuctionService.Core.DTOs.Requests;
using FluentValidation;

namespace AuctionService.Core.Validators;

/// <summary>
/// UpdateAuctionRequest 驗證器
/// </summary>
public class UpdateAuctionRequestValidator : AbstractValidator<UpdateAuctionRequest>
{
    public UpdateAuctionRequestValidator()
    {
        // 商品名稱驗證
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("商品名稱不能為空")
            .Length(3, 200)
            .WithMessage("商品名稱長度必須在 3-200 個字元之間");

        // 商品描述驗證
        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("商品描述不能為空")
            .MaximumLength(2000)
            .WithMessage("商品描述長度不能超過 2000 個字元");

        // 起標價驗證
        RuleFor(x => x.StartingPrice)
            .GreaterThan(0)
            .WithMessage("起標價必須大於 0");

        // 結束時間驗證
        RuleFor(x => x.EndTime)
            .GreaterThan(DateTime.UtcNow.AddHours(1))
            .WithMessage("拍賣結束時間必須至少在 1 小時之後");
    }
}