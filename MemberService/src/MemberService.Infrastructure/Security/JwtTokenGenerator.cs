using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MemberService.Domain.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace MemberService.Infrastructure.Security;

/// <summary>
/// JWT token generator implementation
/// </summary>
public class JwtTokenGenerator : ITokenGenerator
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenExpirationMinutes;
    private readonly int _refreshTokenExpirationDays;

    public JwtTokenGenerator(string secretKey, string issuer = "MemberService", string audience = "MemberService", int accessTokenExpirationMinutes = 15, int refreshTokenExpirationDays = 7)
    {
        _secretKey = secretKey;
        _issuer = issuer;
        _audience = audience;
        _accessTokenExpirationMinutes = accessTokenExpirationMinutes;
        _refreshTokenExpirationDays = refreshTokenExpirationDays;
    }

    public string GenerateAccessToken(long userId, string email)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var now = DateTime.UtcNow;
        
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(_accessTokenExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        // Generate a random refresh token
        var randomBytes = new byte[64];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        return Convert.ToBase64String(randomBytes);
    }

    public bool ValidateToken(string token)
    {
        var (isValid, _, _) = ValidateAndExtractClaims(token);
        return isValid;
    }

    public (bool IsValid, long? UserId, DateTime? ExpiresAt) ValidateAndExtractClaims(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler
            {
                MapInboundClaims = false
            };
            
            // Ensure the token handler can read the token
            if (!tokenHandler.CanReadToken(token))
            {
                return (false, null, null);
            }

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _issuer,
                ValidAudience = _audience,
                IssuerSigningKey = securityKey,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            var jwtToken = (JwtSecurityToken)validatedToken;

            var userIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub);
            var expiresAtClaim = jwtToken.ValidTo;

            if (userIdClaim != null && long.TryParse(userIdClaim.Value, out var userId))
            {
                return (true, userId, expiresAtClaim);
            }

            return (false, null, null);
        }
        catch
        {
            return (false, null, null);
        }
    }
}