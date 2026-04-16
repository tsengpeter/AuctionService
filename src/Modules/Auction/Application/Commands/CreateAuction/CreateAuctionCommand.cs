using MediatR;

namespace Auction.Application.Commands.CreateAuction;

public record CreateAuctionCommand(
    Guid OwnerId,
    string Title,
    string? Description,
    decimal StartingPrice,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    Guid? CategoryId,
    List<string>? ImageUrls) : IRequest<Guid>;
