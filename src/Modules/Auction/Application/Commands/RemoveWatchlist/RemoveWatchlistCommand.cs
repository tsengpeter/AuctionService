using MediatR;

namespace Auction.Application.Commands.RemoveWatchlist;

public record RemoveWatchlistCommand(Guid UserId, Guid AuctionId) : IRequest;
