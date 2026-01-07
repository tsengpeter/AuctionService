using System.Net.Http.Headers;
using System.Text.Json;
using BiddingService.Core.DTOs.Responses;
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

    public async Task<TokenValidationResult> ValidateTokenAsync(string token)
    {
        try
        {
            var requestUri = $"/api/auth/validate?token={Uri.EscapeDataString(token)}";
            var response = await _httpClient.GetAsync(requestUri);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Token validation failed. Status Code: {StatusCode}", response.StatusCode);
                return TokenValidationResult.Failure("Invalid or expired token.");
            }

            var content = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(content);
            
            var root = document.RootElement;
            
            // Extract isValid
            bool isValid = false;
            if (root.TryGetProperty("isValid", out var isValidElement))
            {
                isValid = isValidElement.GetBoolean();
            }
            
            // Extract userId (can be null)
            long? userId = null;
            if (root.TryGetProperty("userId", out var userIdElement) && 
                userIdElement.ValueKind != JsonValueKind.Null &&
                userIdElement.TryGetInt64(out var parsedUserId))
            {
                userId = parsedUserId;
            }
            
            // Extract expiresAt (can be null)
            DateTime? expiresAt = null;
            if (root.TryGetProperty("expiresAt", out var expiresAtElement) && 
                expiresAtElement.ValueKind != JsonValueKind.Null)
            {
                var expiresAtStr = expiresAtElement.GetString();
                if (expiresAtStr != null && DateTime.TryParse(expiresAtStr, out var parsedExpiresAt))
                {
                    expiresAt = parsedExpiresAt;
                }
            }
            
            // Extract errorMessage (can be null)
            string? errorMessage = null;
            if (root.TryGetProperty("errorMessage", out var errorMessageElement) && 
                errorMessageElement.ValueKind != JsonValueKind.Null)
            {
                errorMessage = errorMessageElement.GetString();
            }
            
            var result = new TokenValidationResult
            {
                IsValid = isValid,
                UserId = userId,
                ExpiresAt = expiresAt,
                ErrorMessage = errorMessage
            };
            
            if (isValid)
            {
                _logger.LogInformation("Token validated successfully for user {UserId}", userId);
            }
            else
            {
                _logger.LogWarning("Token validation failed: {ErrorMessage}", errorMessage ?? "Unknown error");
            }
            
            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse JSON response from Member Service");
            return TokenValidationResult.Failure("Invalid response from authentication service.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error connecting to Member Service for token validation.");
            return TokenValidationResult.Failure("Authentication service unavailable.");
        }
    }
}
