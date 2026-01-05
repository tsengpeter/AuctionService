using System.Threading.Tasks;

namespace BiddingService.Core.Interfaces;

public interface IMemberServiceClient
{
    /// <summary>
    /// Validates the JWT token with Member Service and returns the User ID (BidderId).
    /// </summary>
    /// <param name="token">The JWT access token (without "Bearer " prefix).</param>
    /// <returns>The User ID if valid.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown if token is invalid or expired.</exception>
    Task<long> ValidateTokenAsync(string token);
}
