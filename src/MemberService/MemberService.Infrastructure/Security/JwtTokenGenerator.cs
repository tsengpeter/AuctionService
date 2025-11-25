using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MemberService.Domain.Interfaces;

namespace MemberService.Infrastructure.Security;

/// <summary>
/// JWT token generator using HS256 algorithm.
/// Generates access tokens with 15-minute expiration.
/// </summary>
public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _configuration;
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expirationMinutes;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _secretKey = _configuration["Jwt:SecretKey"]
            ?? throw new InvalidOperationException("Jwt:SecretKey is not configured");
        _issuer = _configuration["Jwt:Issuer"] ?? "member-service";
        _audience = _configuration["Jwt:Audience"] ?? "auction-api";
        _expirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "15");
    }

    /// <summary>
    /// Generates a JWT access token.
    /// </summary>
    /// <param name="userId">The user's ID</param>
    /// <param name="email">The user's email</param>
    /// <returns>JWT token string</returns>
    public string GenerateToken(long userId, string email)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim("sub", userId.ToString()), // Subject claim
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expirationMinutes),
            signingCredentials: credentials
        );

        var tokenHandler = new JwtSecurityTokenHandler();
        return tokenHandler.WriteToken(token);
    }
}
