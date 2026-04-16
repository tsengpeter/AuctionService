using MediatR;

namespace Auction.Application.Commands.AddWatchlist;

public record AddWatchlistCommand(Guid UserId, Guid AuctionId) : IRequest;
