using BiddingService.Core.DTOs.Requests;
using FluentValidation;

namespace BiddingService.Core.Validators;

public class CreateBidRequestValidator : AbstractValidator<CreateBidRequest>
{
    public CreateBidRequestValidator()
    {
        RuleFor(x => x.AuctionId)
            .GreaterThan(0)
            .WithMessage("AuctionId must be greater than 0");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than 0")
            .PrecisionScale(18, 2, true)
            .WithMessage("Amount must have at most 2 decimal places");
    }
}