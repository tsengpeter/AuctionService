using BiddingService.Core.Interfaces;
using StackExchange.Redis;

namespace BiddingService.Infrastructure.Redis;

public class RedisConnection : IRedisConnection
{
    public IConnectionMultiplexer Connection { get; }

    public RedisConnection(string connectionString)
    {
        Connection = ConnectionMultiplexer.Connect(connectionString);
    }

    object IRedisConnection.Connection => Connection;
}
