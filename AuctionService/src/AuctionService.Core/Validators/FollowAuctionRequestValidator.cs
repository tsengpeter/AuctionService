using AuctionService.Core.DTOs.Requests;
using FluentValidation;

namespace AuctionService.Core.Validators;

/// <summary>
/// 追蹤商品請求驗證器
/// </summary>
public class FollowAuctionRequestValidator : AbstractValidator<FollowAuctionRequest>
{
    public FollowAuctionRequestValidator()
    {
        RuleFor(x => x.AuctionId)
            .NotEmpty().WithMessage("AuctionId is required");
    }
}