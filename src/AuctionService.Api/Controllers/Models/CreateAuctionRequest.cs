namespace AuctionService.Api.Controllers.Models;

public record CreateAuctionRequest(
    string Title,
    string? Description,
    decimal StartingPrice,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    Guid? CategoryId,
    List<string>? ImageUrls);
