using Auction.Application.Abstractions;
using Auction.Infrastructure.Persistence;
using AuctionService.Shared.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auction.Application.Queries.GetAuctionDetail;

public class GetAuctionDetailQueryHandler(AuctionDbContext db, IBiddingQueryService biddingService)
    : IRequestHandler<GetAuctionDetailQuery, AuctionDetailDto>
{
    public async Task<AuctionDetailDto> Handle(GetAuctionDetailQuery request, CancellationToken cancellationToken)
    {
        var auction = await db.Auctions
            .Include(a => a.Images)
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Auction", request.Id);

        var bidInfo = await biddingService.GetHighestBidAsync(auction.Id, cancellationToken);

        return new AuctionDetailDto
        {
            Id = auction.Id,
            Title = auction.Title,
            Description = auction.Description,
            StartingPrice = auction.StartingPrice,
            CurrentHighestBid = bidInfo?.Amount,
            StartTime = auction.StartTime,
            EndTime = auction.EndTime,
            Status = auction.Status.ToString(),
            OwnerId = auction.OwnerId,
            CategoryId = auction.CategoryId,
            Images = auction.Images
                .OrderBy(i => i.DisplayOrder)
                .Select(i => new AuctionImageDto(i.Id, i.Url, i.DisplayOrder))
                .ToList()
        };
    }
}
