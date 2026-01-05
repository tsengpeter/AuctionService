using System.Net.Http.Headers;
using System.Text.Json;
using BiddingService.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace BiddingService.Infrastructure.HttpClients;

public class MemberServiceClient : IMemberServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MemberServiceClient> _logger;

    public MemberServiceClient(HttpClient httpClient, ILogger<MemberServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<long> ValidateTokenAsync(string token)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/validate");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Token validation failed. Status Code: {StatusCode}", response.StatusCode);
                throw new UnauthorizedAccessException("Invalid or expired token.");
            }

            var content = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(content);
            
            // Assuming the response structure is: { "isValid": true, "userId": 123456789 }
            // Adjust property name based on actual MemberService response
            if (document.RootElement.TryGetProperty("userId", out var userIdElement) && userIdElement.TryGetInt64(out var userId))
            {
                return userId;
            }
            
            // Fallback: Check if it's wrapped in a 'data' property like standard API response
            if (document.RootElement.TryGetProperty("data", out var dataElement) && 
                dataElement.TryGetProperty("userId", out var dataUserIdElement) &&
                dataUserIdElement.TryGetInt64(out var dataUserId))
            {
                return dataUserId;
            }

            _logger.LogError("Failed to parse UserID from validation response: {Response}", content);
            throw new UnauthorizedAccessException("Token valid but user ID missing.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error connecting to Member Service for token validation.");
            throw new Exception("Authentication service unavailable.", ex);
        }
    }
}
