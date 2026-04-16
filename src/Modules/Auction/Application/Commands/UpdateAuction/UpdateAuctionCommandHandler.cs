using Auction.Application.Queries.GetAuctionDetail;
using Auction.Infrastructure.Persistence;
using AuctionService.Shared.Exceptions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auction.Application.Commands.UpdateAuction;

public class UpdateAuctionCommandHandler(AuctionDbContext db) : IRequestHandler<UpdateAuctionCommand, AuctionDetailDto>
{
    public async Task<AuctionDetailDto> Handle(UpdateAuctionCommand request, CancellationToken cancellationToken)
    {
        var auction = await db.Auctions
            .Include(a => a.Images)
            .FirstOrDefaultAsync(a => a.Id == request.AuctionId, cancellationToken)
            ?? throw new NotFoundException("Auction", request.AuctionId);

        if (auction.Status == Domain.Entities.AuctionStatus.Ended)
            throw new ConflictException("Cannot update an ended auction.");

        if (auction.OwnerId != request.RequesterId)
            throw new ForbiddenException();

        var now = DateTimeOffset.UtcNow;

        if (now < auction.StartTime)
        {
            // Before bidding opens: allow updating all fields
            auction.UpdateAll(
                request.Title ?? auction.Title,
                request.Description ?? auction.Description,
                request.StartingPrice ?? auction.StartingPrice,
                request.StartTime ?? auction.StartTime,
                request.EndTime ?? auction.EndTime,
                request.CategoryId ?? auction.CategoryId,
                request.ImageUrls);
        }
        else
        {
            // After bidding has started: only non-sensitive fields allowed
            var sensitiveFields = new List<ValidationFailure>();

            if (request.StartingPrice.HasValue)
                sensitiveFields.Add(new ValidationFailure("StartingPrice", "Cannot modify StartingPrice after bidding has started."));
            if (request.StartTime.HasValue)
                sensitiveFields.Add(new ValidationFailure("StartTime", "Cannot modify StartTime after bidding has started."));
            if (request.EndTime.HasValue)
                sensitiveFields.Add(new ValidationFailure("EndTime", "Cannot modify EndTime after bidding has started."));

            if (sensitiveFields.Count > 0)
                throw new ValidationException(sensitiveFields);

            auction.UpdateNonSensitive(
                request.Title,
                request.Description,
                request.CategoryId,
                request.ImageUrls);
        }

        await db.SaveChangesAsync(cancellationToken);

        return new AuctionDetailDto
        {
            Id = auction.Id,
            Title = auction.Title,
            Description = auction.Description,
            StartingPrice = auction.StartingPrice,
            CurrentHighestBid = null,
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
