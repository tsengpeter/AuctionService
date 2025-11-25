using FluentAssertions;
using MemberService.Domain.Entities;
using MemberService.Domain.Exceptions;
using MemberService.Domain.ValueObjects;
using MemberService.Infrastructure.Persistence;
using MemberService.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MemberService.Infrastructure.Tests.Persistence;

/// <summary>
/// Unit tests for UserRepository using in-memory database.
/// </summary>
public class UserRepositoryTests : IDisposable
{
    private readonly MemberDbContext _context;
    private readonly UserRepository _repository;

    public UserRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<MemberDbContext>()
            .UseInMemoryDatabase(databaseName: $"MemberTestDb_{Guid.NewGuid()}")
            .Options;

        _context = new MemberDbContext(options);
        _repository = new UserRepository(_context);
    }

    [Fact]
    public async Task FindByEmailAsync_WithValidEmail_ReturnsUser()
    {
        // Arrange
        var email = Email.Create("test@example.com");
        var username = Username.Create("test user");
        var user = User.Create(1L, email, username, "hashedpwd");

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.FindByEmailAsync("test@example.com");

        // Assert
        result.Should().NotBeNull();
        result!.Email.Value.Should().Be("test@example.com");
        result.Username.Value.Should().Be("test user");
    }

    [Fact]
    public async Task FindByEmailAsync_WithNonExistentEmail_ReturnsNull()
    {
        // Act
        var result = await _repository.FindByEmailAsync("nonexistent@example.com");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindByIdAsync_WithValidId_ReturnsUser()
    {
        // Arrange
        var email = Email.Create("test@example.com");
        var username = Username.Create("test user");
        var user = User.Create(1L, email, username, "hashedpwd");

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.FindByIdAsync(1L);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1L);
    }

    [Fact]
    public async Task FindByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Act
        var result = await _repository.FindByIdAsync(999L);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsUser()
    {
        // Arrange
        var email = Email.Create("test@example.com");
        var username = Username.Create("test user");
        var user = User.Create(1L, email, username, "hashedpwd");

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(1L);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1L);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ThrowsUserNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<UserNotFoundException>(
            () => _repository.GetByIdAsync(999L));
    }

    [Fact]
    public async Task AddAsync_WithValidUser_SavesSuccessfully()
    {
        // Arrange
        var email = Email.Create("newuser@example.com");
        var username = Username.Create("new user");
        var user = User.Create(2L, email, username, "hashedpwd");

        // Act
        await _repository.AddAsync(user);
        await _context.SaveChangesAsync();

        // Assert
        var savedUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == 2L);
        savedUser.Should().NotBeNull();
        savedUser!.Email.Value.Should().Be("newuser@example.com");
    }

    [Fact]
    public async Task AddAsync_WithDuplicateEmail_ThrowsEmailAlreadyExistsException()
    {
        // Arrange
        var email = Email.Create("duplicate@example.com");
        var username1 = Username.Create("user one");
        var username2 = Username.Create("user two");

        var user1 = User.Create(1L, email, username1, "hashedpwd");
        await _repository.AddAsync(user1);
        await _context.SaveChangesAsync();

        var user2 = User.Create(2L, email, username2, "hashedpwd");

        // Act & Assert
        await Assert.ThrowsAsync<EmailAlreadyExistsException>(
            () => _repository.AddAsync(user2));
    }

    [Fact]
    public async Task UpdateAsync_WithValidUser_UpdatesSuccessfully()
    {
        // Arrange
        var email = Email.Create("test@example.com");
        var username = Username.Create("test user");
        var user = User.Create(1L, email, username, "hashedpwd");

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var newUsername = Username.Create("updated user");
        user.UpdateUsername(newUsername);

        // Act
        await _repository.UpdateAsync(user);
        await _context.SaveChangesAsync();

        // Assert
        var updatedUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == 1L);
        updatedUser!.Username.Value.Should().Be("updated user");
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
