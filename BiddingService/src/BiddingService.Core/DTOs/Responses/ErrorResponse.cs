namespace BiddingService.Core.DTOs.Responses;

public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public Dictionary<string, string[]>? Errors { get; set; }
}