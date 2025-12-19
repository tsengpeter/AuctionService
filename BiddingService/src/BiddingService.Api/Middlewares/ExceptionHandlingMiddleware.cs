using BiddingService.Core.DTOs.Responses;
using BiddingService.Core.Exceptions;
using BiddingService.Shared.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace BiddingService.Api.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> _logger)
    {
        _next = next;
        this._logger = _logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AuctionNotFoundException ex)
        {
            _logger.LogWarning(ex, "Auction not found: {AuctionId}", ex.AuctionId);

            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            context.Response.ContentType = "application/json";

            var response = new ErrorResponse
            {
                ErrorCode = ErrorCodes.AUCTION_NOT_FOUND,
                Message = ex.Message,
                CorrelationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? string.Empty
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
        catch (AuctionNotActiveException ex)
        {
            _logger.LogWarning(ex, "Auction not active: {AuctionId}", ex.AuctionId);

            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";

            var response = new ErrorResponse
            {
                ErrorCode = ErrorCodes.AUCTION_NOT_ACTIVE,
                Message = ex.Message,
                CorrelationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? string.Empty
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
        catch (BidAmountTooLowException ex)
        {
            _logger.LogWarning(ex, "Bid amount too low. Current: {CurrentHighest}, Bid: {BidAmount}",
                ex.CurrentHighestBid, ex.BidAmount);

            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";

            var response = new ErrorResponse
            {
                ErrorCode = ErrorCodes.BID_AMOUNT_TOO_LOW,
                Message = ex.Message,
                CorrelationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? string.Empty
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
        catch (DuplicateBidException ex)
        {
            _logger.LogWarning(ex, "Duplicate bid attempt. Auction: {AuctionId}, Bidder: {BidderId}",
                ex.AuctionId, ex.BidderId);

            context.Response.StatusCode = (int)HttpStatusCode.Conflict;
            context.Response.ContentType = "application/json";

            var response = new ErrorResponse
            {
                ErrorCode = ErrorCodes.DUPLICATE_BID,
                Message = ex.Message,
                CorrelationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? string.Empty
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var response = new ErrorResponse
            {
                ErrorCode = ErrorCodes.INTERNAL_SERVER_ERROR,
                Message = "An internal server error occurred",
                CorrelationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? string.Empty
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
