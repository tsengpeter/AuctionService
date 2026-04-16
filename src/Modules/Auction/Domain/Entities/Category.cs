using AuctionService.Shared;

namespace Auction.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public Guid? ParentId { get; private set; }

    private Category() { }

    public static Category Create(string name, Guid? parentId = null) =>
        new()
        {
            Name = name,
            ParentId = parentId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
}
