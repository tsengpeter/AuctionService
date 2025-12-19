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

    public new async Task<Bid?> GetByIdAsync(long id)
    {
        return await base.GetByIdAsync(id);
    }
}