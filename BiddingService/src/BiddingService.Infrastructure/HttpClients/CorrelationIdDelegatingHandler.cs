using System.Net.Http.Headers;

namespace BiddingService.Infrastructure.HttpClients;

public class CorrelationIdDelegatingHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Add correlation ID to outgoing requests
        if (request.Headers.Contains("X-Correlation-ID") == false)
        {
            var correlationId = Guid.NewGuid().ToString();
            request.Headers.Add("X-Correlation-ID", correlationId);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
