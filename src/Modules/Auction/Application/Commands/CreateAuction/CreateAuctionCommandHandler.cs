using Auction.Infrastructure.Persistence;
using MediatR;

using AuctionEntity = Auction.Domain.Entities.Auction;

namespace Auction.Application.Commands.CreateAuction;

public class CreateAuctionCommandHandler(AuctionDbContext db) : IRequestHandler<CreateAuctionCommand, Guid>
{
    public async Task<Guid> Handle(CreateAuctionCommand request, CancellationToken cancellationToken)
    {
        var auction = AuctionEntity.Create(
            request.OwnerId,
            request.Title,
            request.Description,
            request.StartingPrice,
            request.StartTime,
            request.EndTime,
            request.CategoryId,
            request.ImageUrls);

        db.Auctions.Add(auction);
        await db.SaveChangesAsync(cancellationToken);

        return auction.Id;
    }
}
