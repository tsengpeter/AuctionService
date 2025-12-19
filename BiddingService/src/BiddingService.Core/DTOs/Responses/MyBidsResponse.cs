namespace BiddingService.Core.DTOs.Responses;

public class MyBidsResponse
{
    public IEnumerable<MyBidResponse> Bids { get; set; } = new List<MyBidResponse>();
    public PaginationMetadata Pagination { get; set; } = new PaginationMetadata();
}