using MediatR;

namespace Auction.Application.Queries.GetWatchlist;

public record GetWatchlistQuery(Guid UserId, string? Status) : IRequest<List<WatchlistItemDto>>;
