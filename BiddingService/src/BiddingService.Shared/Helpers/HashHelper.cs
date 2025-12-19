using System.Security.Cryptography;
using System.Text;

namespace BiddingService.Shared.Helpers;

public static class HashHelper
{
    public static string ComputeSha256Hash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}