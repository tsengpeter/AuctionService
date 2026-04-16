using Auction.Domain.Entities;
using Auction.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auction.Application.Queries.GetWatchlist;

public class GetWatchlistQueryHandler(AuctionDbContext db) : IRequestHandler<GetWatchlistQuery, List<WatchlistItemDto>>
{
    public async Task<List<WatchlistItemDto>> Handle(GetWatchlistQuery request, CancellationToken cancellationToken)
    {
        var query = db.Watchlist
            .Where(w => w.UserId == request.UserId)
            .Join(db.Auctions,
                w => w.AuctionId,
                a => a.Id,
                (w, a) => new { Watchlist = w, Auction = a });

        if (string.Equals(request.Status, "active", StringComparison.OrdinalIgnoreCase))
            query = query.Where(x => x.Auction.Status == AuctionStatus.Active);

        var results = await query
            .OrderByDescending(x => x.Watchlist.CreatedAt)
            .Select(x => new WatchlistItemDto
            {
                AuctionId = x.Auction.Id,
                Title = x.Auction.Title,
                StartingPrice = x.Auction.StartingPrice,
                StartTime = x.Auction.StartTime,
                EndTime = x.Auction.EndTime,
                Status = x.Auction.Status.ToString(),
                ThumbnailUrl = x.Auction.Images
                    .OrderBy(i => i.DisplayOrder)
                    .Select(i => i.Url)
                    .FirstOrDefault(),
                AddedAt = x.Watchlist.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return results;
    }
}
