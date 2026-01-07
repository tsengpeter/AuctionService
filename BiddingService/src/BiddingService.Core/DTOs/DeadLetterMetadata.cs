using BiddingService.Core.Entities;

namespace BiddingService.Core.DTOs;

public class DeadLetterMetadata
{
    public Bid Bid { get; set; } = null!;
    public int RetryCount { get; set; }
    public DateTime FirstFailedAt { get; set; }
    public DateTime LastFailedAt { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public int MaxRetries { get; set; } = 3;

    public bool ShouldRetry() => RetryCount < MaxRetries;
}
