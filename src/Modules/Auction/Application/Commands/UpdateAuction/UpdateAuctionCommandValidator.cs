using FluentValidation;

namespace Auction.Application.Commands.UpdateAuction;

public class UpdateAuctionCommandValidator : AbstractValidator<UpdateAuctionCommand>
{
    public UpdateAuctionCommandValidator()
    {
        When(x => x.Title is not null, () =>
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200));

        When(x => x.StartingPrice.HasValue, () =>
            RuleFor(x => x.StartingPrice!.Value).GreaterThan(0));

        When(x => x.StartTime.HasValue, () =>
            RuleFor(x => x.StartTime!.Value).GreaterThan(DateTimeOffset.UtcNow)
                .WithMessage("StartTime must be in the future."));

        When(x => x.EndTime.HasValue && x.StartTime.HasValue, () =>
            RuleFor(x => x.EndTime!.Value)
                .GreaterThan(x => x.StartTime!.Value.AddMinutes(1))
                .WithMessage("EndTime must be at least 1 minute after StartTime."));

        When(x => x.ImageUrls is not null, () =>
            RuleFor(x => x.ImageUrls)
                .Must(urls => urls == null || urls.Count <= 5)
                .WithMessage("ImageUrls cannot exceed 5 items.")
                .ForEach(rule => rule
                    .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
                    .WithMessage("Each image URL must be a valid absolute URI.")));
    }
}
