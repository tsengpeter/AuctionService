using FluentAssertions;
using MemberService.Domain.Entities;
using MemberService.Domain.ValueObjects;
using MemberService.Infrastructure.Persistence;
using MemberService.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MemberService.Infrastructure.Tests.Persistence;

/// <summary>
/// Unit tests for RefreshTokenRepository using in-memory database.
/// </summary>
public class RefreshTokenRepositoryTests : IDisposable
{
    private readonly MemberDbContext _context;
    private readonly RefreshTokenRepository _repository;

    public RefreshTokenRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<MemberDbContext>()
            .UseInMemoryDatabase(databaseName: $"MemberTestDb_{Guid.NewGuid()}")
            .Options;

        _context = new MemberDbContext(options);
        _repository = new RefreshTokenRepository(_context);
    }

    [Fact]
    public async Task FindByTokenAsync_WithValidToken_ReturnsRefreshToken()
    {
        // Arrange
        var email = Email.Create("test@example.com");
        var username = Username.Create("testuser");
        var user = User.Create(1L, email, username, "hashedpwd");
        
        var token = RefreshToken.Create(
            token: "valid_refresh_token_base64_encoded",
            userId: 1L,
            expiresAt: DateTime.UtcNow.AddDays(7)
        );

        _context.Users.Add(user);
        _context.RefreshTokens.Add(token);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.FindByTokenAsync("valid_refresh_token_base64_encoded");

        // Assert
        result.Should().NotBeNull();
        result!.Token.Should().Be("valid_refresh_token_base64_encoded");
        result.UserId.Should().Be(1L);
    }

    [Fact]
    public async Task FindByTokenAsync_WithNonExistentToken_ReturnsNull()
    {
        // Act
        var result = await _repository.FindByTokenAsync("nonexistent_token");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_WithValidToken_SavesSuccessfully()
    {
        // Arrange
        var email = Email.Create("test@example.com");
        var username = Username.Create("testuser");
        var user = User.Create(1L, email, username, "hashedpwd");
        
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token = RefreshToken.Create(
            token: "new_refresh_token",
            userId: 1L,
            expiresAt: DateTime.UtcNow.AddDays(7)
        );

        // Act
        await _repository.AddAsync(token);
        await _context.SaveChangesAsync();

        // Assert
        var savedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == "new_refresh_token");
        savedToken.Should().NotBeNull();
    }

    [Fact]
    public async Task RevokeAllForUserAsync_RevokesAllNonRevokedTokens()
    {
        // Arrange
        var email = Email.Create("test@example.com");
        var username = Username.Create("testuser");
        var user = User.Create(1L, email, username, "hashedpwd");
        
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token1 = RefreshToken.Create(
            token: "token1",
            userId: 1L,
            expiresAt: DateTime.UtcNow.AddDays(7)
        );

        var token2 = RefreshToken.Create(
            token: "token2",
            userId: 1L,
            expiresAt: DateTime.UtcNow.AddDays(7)
        );

        _context.RefreshTokens.AddRange(token1, token2);
        await _context.SaveChangesAsync();

        // Act
        await _repository.RevokeAllForUserAsync(1L);
        await _context.SaveChangesAsync();

        // Assert
        var allTokens = await _context.RefreshTokens
            .Where(t => t.UserId == 1L && !t.IsRevoked)
            .ToListAsync();
        allTokens.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveExpiredAsync_DeletesExpiredTokens()
    {
        // Arrange
        var email = Email.Create("test@example.com");
        var username = Username.Create("testuser");
        var user = User.Create(1L, email, username, "hashedpwd");
        
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var expiredToken = RefreshToken.Create(
            token: "expired_token",
            userId: 1L,
            expiresAt: DateTime.UtcNow.AddDays(-1) // Expired
        );

        var validToken = RefreshToken.Create(
            token: "valid_token",
            userId: 1L,
            expiresAt: DateTime.UtcNow.AddDays(7)
        );

        _context.RefreshTokens.AddRange(expiredToken, validToken);
        await _context.SaveChangesAsync();

        // Act
        await _repository.RemoveExpiredAsync();
        await _context.SaveChangesAsync();

        // Assert
        var remainingTokens = await _context.RefreshTokens.ToListAsync();
        remainingTokens.Should().HaveCount(1);
        remainingTokens.First().Token.Should().Be("valid_token");
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
