using Prometheus;

namespace BiddingService.Api.Metrics;

public static class BiddingMetrics
{
    // Counter for total bid requests
    public static readonly Counter BidRequestsTotal = Prometheus.Metrics
        .CreateCounter("bid_requests_total", "Total number of bid requests",
            new CounterConfiguration
            {
                LabelNames = new[] { "method", "endpoint", "status" }
            });

    // Histogram for bid request latency
    public static readonly Histogram BidRequestDuration = Prometheus.Metrics
        .CreateHistogram("bid_request_duration_seconds", "Duration of bid requests in seconds",
            new HistogramConfiguration
            {
                LabelNames = new[] { "method", "endpoint" },
                Buckets = Histogram.ExponentialBuckets(0.01, 2, 10) // 10ms to ~10s
            });

    // Gauge for Redis fallback status
    public static readonly Gauge RedisFallbackActive = Prometheus.Metrics
        .CreateGauge("redis_fallback_active", "Whether Redis fallback is currently active (1 = active, 0 = normal)");

    // Counter for Redis operations
    public static readonly Counter RedisOperationsTotal = Prometheus.Metrics
        .CreateCounter("redis_operations_total", "Total number of Redis operations",
            new CounterConfiguration
            {
                LabelNames = new[] { "operation", "status" }
            });

    // Counter for database operations
    public static readonly Counter DatabaseOperationsTotal = Prometheus.Metrics
        .CreateCounter("database_operations_total", "Total number of database operations",
            new CounterConfiguration
            {
                LabelNames = new[] { "operation", "status" }
            });

    // Histogram for auction statistics query duration
    public static readonly Histogram AuctionStatsQueryDuration = Prometheus.Metrics
        .CreateHistogram("auction_stats_query_duration_seconds", "Duration of auction statistics queries",
            new HistogramConfiguration
            {
                Buckets = Histogram.ExponentialBuckets(0.01, 2, 10)
            });
}
