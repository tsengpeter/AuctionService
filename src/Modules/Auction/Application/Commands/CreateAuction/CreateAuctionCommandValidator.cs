using FluentValidation;

namespace Auction.Application.Commands.CreateAuction;

public class CreateAuctionCommandValidator : AbstractValidator<CreateAuctionCommand>
{
    public CreateAuctionCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.StartingPrice)
            .GreaterThan(0);

        RuleFor(x => x.StartTime)
            .GreaterThan(DateTimeOffset.UtcNow)
            .WithMessage("StartTime must be in the future.");

        RuleFor(x => x.EndTime)
            .GreaterThan(x => x.StartTime.AddMinutes(1))
            .WithMessage("EndTime must be at least 1 minute after StartTime.");

        RuleFor(x => x.ImageUrls)
            .Must(urls => urls == null || urls.Count <= 5)
            .WithMessage("ImageUrls cannot exceed 5 items.")
            .ForEach(rule => rule
                .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
                .WithMessage("Each image URL must be a valid absolute URI."))
            .When(x => x.ImageUrls is { Count: > 0 });
    }
}
