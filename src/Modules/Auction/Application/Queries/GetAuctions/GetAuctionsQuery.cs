using Auction.Application.DTOs;
using FluentValidation;
using MediatR;

namespace Auction.Application.Queries.GetAuctions;

public class GetAuctionsQuery : IRequest<PagedResult<AuctionListItemDto>>
{
    public string? Q { get; init; }
    public Guid? CategoryId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public class GetAuctionsQueryValidator : AbstractValidator<GetAuctionsQuery>
{
    public GetAuctionsQueryValidator()
    {
        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100.");

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be >= 1.");
    }
}
