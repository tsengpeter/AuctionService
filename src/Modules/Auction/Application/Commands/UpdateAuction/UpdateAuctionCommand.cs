using Auction.Application.Queries.GetAuctionDetail;
using MediatR;

namespace Auction.Application.Commands.UpdateAuction;

public record UpdateAuctionCommand(
    Guid AuctionId,
    Guid RequesterId,
    string? Title,
    string? Description,
    decimal? StartingPrice,
    DateTimeOffset? StartTime,
    DateTimeOffset? EndTime,
    Guid? CategoryId,
    List<string>? ImageUrls) : IRequest<AuctionDetailDto>;
