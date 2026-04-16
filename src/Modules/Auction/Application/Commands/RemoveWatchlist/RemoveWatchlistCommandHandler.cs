using Auction.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auction.Application.Commands.RemoveWatchlist;

public class RemoveWatchlistCommandHandler(AuctionDbContext db) : IRequestHandler<RemoveWatchlistCommand>
{
    public async Task Handle(RemoveWatchlistCommand request, CancellationToken cancellationToken)
    {
        var entry = await db.Watchlist.FirstOrDefaultAsync(
            w => w.UserId == request.UserId && w.AuctionId == request.AuctionId, cancellationToken);

        if (entry is not null)
        {
            db.Watchlist.Remove(entry);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
