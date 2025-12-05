using FluentAssertions;
using MemberService.Domain.Entities;
using MemberService.Domain.ValueObjects;
using MemberService.Infrastructure.Persistence;
using MemberService.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MemberService.Infrastructure.Tests.Persistence;

public class RefreshTokenRepositoryTests : IDisposable
{
    private readonly MemberDbContext _context;
    private readonly RefreshTokenRepository _repository;

    public RefreshTokenRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<MemberDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new MemberDbContext(options);
        _repository = new RefreshTokenRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task AddAsync_ShouldAddRefreshTokenToDatabase()
    {
        // Arrange
        var tokenId = Guid.NewGuid();
        var token = "test-refresh-token";
        var userId = 12345L;
        var expiresAt = DateTime.UtcNow.AddDays(7);

        var refreshToken = new RefreshToken(tokenId, token, userId, expiresAt);

        // Act
        await _repository.AddAsync(refreshToken);

        // Assert
        var savedToken = await _context.RefreshTokens.FindAsync(tokenId);
        savedToken.Should().NotBeNull();
        savedToken!.Token.Should().Be(token);
        savedToken.UserId.Should().Be(userId);
        savedToken.ExpiresAt.Should().BeCloseTo(expiresAt, TimeSpan.FromSeconds(1));
        savedToken.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task GetByTokenAsync_ShouldReturnRefreshToken_WhenTokenExists()
    {
        // Arrange
        var tokenId = Guid.NewGuid();
        var token = "test-refresh-token";
        var userId = 12345L;
        var expiresAt = DateTime.UtcNow.AddDays(7);

        var refreshToken = new RefreshToken(tokenId, token, userId, expiresAt);
        await _context.RefreshTokens.AddAsync(refreshToken);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByTokenAsync(token);

        // Assert
        result.Should().NotBeNull();
        result!.Token.Should().Be(token);
        result.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task GetByTokenAsync_ShouldReturnNull_WhenTokenDoesNotExist()
    {
        // Act
        var result = await _repository.GetByTokenAsync("nonexistent-token");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetValidTokensByUserIdAsync_ShouldReturnOnlyValidTokens()
    {
        // Arrange
        var userId = 12345L;
        var now = DateTime.UtcNow;

        var validToken1 = new RefreshToken(Guid.NewGuid(), "valid1", userId, now.AddDays(1));
        var validToken2 = new RefreshToken(Guid.NewGuid(), "valid2", userId, now.AddHours(1));
        var expiredToken = new RefreshToken(Guid.NewGuid(), "expired", userId, now.AddMinutes(-1));
        var revokedToken = new RefreshToken(Guid.NewGuid(), "revoked", userId, now.AddDays(1));

        revokedToken.Revoke();

        await _context.RefreshTokens.AddRangeAsync(validToken1, validToken2, expiredToken, revokedToken);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetValidTokensByUserIdAsync(userId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(t => t.Token == "valid1");
        result.Should().Contain(t => t.Token == "valid2");
        result.Should().NotContain(t => t.Token == "expired");
        result.Should().NotContain(t => t.Token == "revoked");
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateRefreshTokenInDatabase()
    {
        // Arrange
        var tokenId = Guid.NewGuid();
        var token = "test-refresh-token";
        var userId = 12345L;
        var expiresAt = DateTime.UtcNow.AddDays(7);

        var refreshToken = new RefreshToken(tokenId, token, userId, expiresAt);
        await _context.RefreshTokens.AddAsync(refreshToken);
        await _context.SaveChangesAsync();

        // Act
        refreshToken.Revoke();
        await _repository.UpdateAsync(refreshToken);

        // Assert
        var updatedToken = await _context.RefreshTokens.FindAsync(tokenId);
        updatedToken.Should().NotBeNull();
        updatedToken!.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeAllUserTokensAsync_ShouldRevokeAllTokensForUser()
    {
        // Arrange
        var userId = 12345L;
        var otherUserId = 67890L;

        var token1 = new RefreshToken(Guid.NewGuid(), "token1", userId, DateTime.UtcNow.AddDays(1));
        var token2 = new RefreshToken(Guid.NewGuid(), "token2", userId, DateTime.UtcNow.AddDays(1));
        var otherUserToken = new RefreshToken(Guid.NewGuid(), "other", otherUserId, DateTime.UtcNow.AddDays(1));

        await _context.RefreshTokens.AddRangeAsync(token1, token2, otherUserToken);
        await _context.SaveChangesAsync();

        // Act
        await _repository.RevokeAllUserTokensAsync(userId);

        // Assert
        var userTokens = await _context.RefreshTokens.Where(rt => rt.UserId == userId).ToListAsync();
        userTokens.Should().AllSatisfy(rt => rt.IsRevoked.Should().BeTrue());

        var otherUserTokenFromDb = await _context.RefreshTokens.FindAsync(otherUserToken.Id);
        otherUserTokenFromDb!.IsRevoked.Should().BeFalse();
    }
}