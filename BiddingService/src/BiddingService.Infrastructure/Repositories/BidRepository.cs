using BiddingService.Core.Entities;
using BiddingService.Core.Interfaces;
using BiddingService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BiddingService.Infrastructure.Repositories;

public class BidRepository : GenericRepository<Bid>, IBidRepository
{
    public BidRepository(BiddingDbContext context) : base(context)
    {
    }

    public async Task<Bid?> GetHighestBidAsync(long auctionId)
    {
        return await _context.Bids
            .Where(b => b.AuctionId == auctionId)
            .OrderByDescending(b => b.Amount)
            .ThenByDescending(b => b.BidAt)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Bid>> GetBidsByAuctionAsync(long auctionId, int page = 1, int pageSize = 50)
    {
        return await _context.Bids
            .Where(b => b.AuctionId == auctionId)
            .OrderByDescending(b => b.BidAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<Bid>> GetBidsByBidderAsync(string bidderId, int page = 1, int pageSize = 50)
    {
        return await _context.Bids
            .Where(b => b.BidderId == bidderId)
            .OrderByDescending(b => b.BidAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<Bid>> GetBidsByBidderIdHashAsync(string bidderIdHash, int page = 1, int pageSize = 50)
    {
        return await _context.Bids
            .Where(b => b.BidderIdHash == bidderIdHash)
            .OrderByDescending(b => b.BidAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetBidsCountByBidderIdHashAsync(string bidderIdHash)
    {
        return await _context.Bids
            .Where(b => b.BidderIdHash == bidderIdHash)
            .CountAsync();
    }

    public async Task<int> GetBidCountAsync(long auctionId)
    {
        return await _context.Bids
            .Where(b => b.AuctionId == auctionId)
            .CountAsync();
    }

    public async Task<AuctionStatsData> GetAuctionStatsAsync(long auctionId)
    {
        var now = DateTime.UtcNow;
        var oneHourAgo = now.AddHours(-1);
        var oneDayAgo = now.AddDays(-1);

        var stats = await _context.Bids
            .Where(b => b.AuctionId == auctionId)
            .GroupBy(b => 1) // Group all bids together
            .Select(g => new AuctionStatsData
            {
                TotalBids = g.Count(),
                UniqueBidders = g.Select(b => b.BidderIdHash).Distinct().Count(),
                AverageBidAmount = g.Average(b => b.Amount),
                FirstBidAt = g.Min(b => b.BidAt),
                LastBidAt = g.Max(b => b.BidAt),
                BidsInLastHour = g.Count(b => b.BidAt >= oneHourAgo),
                BidsInLast24Hours = g.Count(b => b.BidAt >= oneDayAgo)
            })
            .FirstOrDefaultAsync();

        return stats ?? new AuctionStatsData
        {
            TotalBids = 0,
            UniqueBidders = 0,
            AverageBidAmount = null,
            FirstBidAt = null,
            LastBidAt = null,
            BidsInLastHour = 0,
            BidsInLast24Hours = 0
        };
    }

    public new async Task<Bid?> GetByIdAsync(long id)
    {
        return await base.GetByIdAsync(id);
    }
}