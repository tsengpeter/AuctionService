using BiddingService.Api.Metrics;
using System.Diagnostics;

namespace BiddingService.Api.Middlewares;

public class MetricsMiddleware
{
    private readonly RequestDelegate _next;

    public MetricsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        var method = context.Request.Method;

        // Only track API endpoints
        if (path.StartsWith("/api/bids"))
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                await _next(context);

                stopwatch.Stop();

                // Record metrics
                BiddingMetrics.BidRequestsTotal
                    .WithLabels(method, path, context.Response.StatusCode.ToString())
                    .Inc();

                BiddingMetrics.BidRequestDuration
                    .WithLabels(method, path)
                    .Observe(stopwatch.Elapsed.TotalSeconds);
            }
            catch
            {
                stopwatch.Stop();

                // Record failed requests
                BiddingMetrics.BidRequestsTotal
                    .WithLabels(method, path, "500")
                    .Inc();

                BiddingMetrics.BidRequestDuration
                    .WithLabels(method, path)
                    .Observe(stopwatch.Elapsed.TotalSeconds);

                throw;
            }
        }
        else
        {
            await _next(context);
        }
    }
}