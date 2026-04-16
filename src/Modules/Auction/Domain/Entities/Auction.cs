using AuctionService.Shared;

namespace Auction.Domain.Entities;

public enum AuctionStatus { Active, Ended }

public class Auction : BaseEntity
{
    public Guid OwnerId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal StartingPrice { get; private set; }
    public DateTimeOffset StartTime { get; private set; }
    public DateTimeOffset EndTime { get; private set; }
    public AuctionStatus Status { get; private set; } = AuctionStatus.Active;
    public Guid? CategoryId { get; private set; }
    public Guid? WinnerId { get; private set; }
    public decimal? SoldAmount { get; private set; }

    private readonly List<AuctionImage> _images = new();
    public IReadOnlyCollection<AuctionImage> Images => _images.AsReadOnly();

    private Auction() { }

    public static Auction Create(
        Guid ownerId,
        string title,
        string? description,
        decimal startingPrice,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        Guid? categoryId,
        IEnumerable<string>? imageUrls)
    {
        var now = DateTimeOffset.UtcNow;
        var auction = new Auction
        {
            OwnerId = ownerId,
            Title = title.Trim(),
            Description = description?.Trim(),
            StartingPrice = startingPrice,
            StartTime = startTime,
            EndTime = endTime,
            Status = AuctionStatus.Active,
            CategoryId = categoryId,
            CreatedAt = now,
            UpdatedAt = now
        };

        if (imageUrls is not null)
        {
            var urls = imageUrls.ToList();
            for (int i = 0; i < urls.Count; i++)
                auction._images.Add(AuctionImage.Create(auction.Id, urls[i], i + 1));
        }

        return auction;
    }

    public void UpdateAll(
        string title,
        string? description,
        decimal startingPrice,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        Guid? categoryId,
        IEnumerable<string>? imageUrls)
    {
        if (Status == AuctionStatus.Ended)
            throw new InvalidOperationException("Cannot update an ended auction.");
        if (DateTimeOffset.UtcNow >= StartTime)
            throw new InvalidOperationException("Bidding has started; cannot update competitive fields.");

        Title = title.Trim();
        Description = description?.Trim();
        StartingPrice = startingPrice;
        StartTime = startTime;
        EndTime = endTime;
        CategoryId = categoryId;
        ReplaceImages(imageUrls);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateNonSensitive(
        string? title,
        string? description,
        Guid? categoryId,
        IEnumerable<string>? imageUrls)
    {
        if (Status == AuctionStatus.Ended)
            throw new InvalidOperationException("Cannot update an ended auction.");

        if (title is not null) Title = title.Trim();
        if (description is not null) Description = description.Trim();
        if (categoryId is not null) CategoryId = categoryId;
        if (imageUrls is not null) ReplaceImages(imageUrls);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private void ReplaceImages(IEnumerable<string>? imageUrls)
    {
        if (imageUrls is null) return;
        _images.Clear();
        var urls = imageUrls.ToList();
        for (int i = 0; i < urls.Count; i++)
            _images.Add(AuctionImage.Create(Id, urls[i], i + 1));
    }

    public void End(Guid? winnerId, decimal? soldAmount)
    {
        if (Status != AuctionStatus.Active)
            throw new InvalidOperationException($"Cannot end auction in status {Status}.");
        Status = AuctionStatus.Ended;
        WinnerId = winnerId;
        SoldAmount = soldAmount;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
