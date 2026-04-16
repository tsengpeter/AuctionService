using MediatR;

namespace Auction.Application.Queries.GetAuctionDetail;

public record GetAuctionDetailQuery(Guid Id) : IRequest<AuctionDetailDto>;
