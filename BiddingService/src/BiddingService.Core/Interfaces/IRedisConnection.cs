namespace BiddingService.Core.Interfaces;

public interface IRedisConnection
{
    object Connection { get; }
}