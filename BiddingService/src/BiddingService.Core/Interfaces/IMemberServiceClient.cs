using System.Threading.Tasks;
using BiddingService.Core.DTOs.Responses;

namespace BiddingService.Core.Interfaces;

public interface IMemberServiceClient
{
    /// <summary>
    /// Validates the JWT token with Member Service.
    /// </summary>
    /// <param name="token">The JWT access token (without "Bearer " prefix).</param>
    /// <returns>The validation result containing token status and user information.</returns>
    Task<TokenValidationResult> ValidateTokenAsync(string token);
}
