using Auction.Domain.Entities;
using Auction.Infrastructure.Persistence;
using AuctionService.Shared.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auction.Application.Commands.AddWatchlist;

public class AddWatchlistCommandHandler(AuctionDbContext db) : IRequestHandler<AddWatchlistCommand>
{
    public async Task Handle(AddWatchlistCommand request, CancellationToken cancellationToken)
    {
        var auctionExists = await db.Auctions.AnyAsync(a => a.Id == request.AuctionId, cancellationToken);
        if (!auctionExists)
            throw new NotFoundException("Auction", request.AuctionId);

        var alreadyExists = await db.Watchlist.AnyAsync(
            w => w.UserId == request.UserId && w.AuctionId == request.AuctionId, cancellationToken);

        if (!alreadyExists)
        {
            db.Watchlist.Add(Watchlist.Create(request.UserId, request.AuctionId));
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
