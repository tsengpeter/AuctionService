using Auction.Application.DTOs;
using Auction.Domain.Entities;
using Auction.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auction.Application.Queries.GetAuctions;

public class GetAuctionsQueryHandler : IRequestHandler<GetAuctionsQuery, PagedResult<AuctionListItemDto>>
{
    private readonly AuctionDbContext _db;

    public GetAuctionsQueryHandler(AuctionDbContext db) => _db = db;

    public async Task<PagedResult<AuctionListItemDto>> Handle(
        GetAuctionsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Auctions.Where(a => a.Status == AuctionStatus.Active);

        if (!string.IsNullOrWhiteSpace(request.Q))
        {
            var qLower = request.Q.ToLower();
            query = query.Where(a => a.Title.ToLower().Contains(qLower));
        }

        if (request.CategoryId.HasValue)
            query = query.Where(a => a.CategoryId == request.CategoryId.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new AuctionListItemDto
            {
                Id = a.Id,
                Title = a.Title,
                StartingPrice = a.StartingPrice,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                Status = a.Status.ToString(),
                CategoryId = a.CategoryId,
                ThumbnailUrl = a.Images
                    .OrderBy(img => img.DisplayOrder)
                    .Select(img => img.Url)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<AuctionListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
