using FluentAssertions;
using MemberService.Domain.Entities;
using MemberService.Domain.ValueObjects;
using MemberService.Infrastructure.Persistence;
using MemberService.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MemberService.Infrastructure.Tests.Persistence;

public class UserRepositoryTests : IDisposable
{
    private readonly MemberDbContext _context;
    private readonly UserRepository _repository;

    public UserRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<MemberDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new MemberDbContext(options);
        _repository = new UserRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task AddAsync_ShouldAddUserToDatabase()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value;
        var username = Username.Create("testuser").Value;
        var passwordHash = "hashedpassword";
        var user = new User(12345L, email, passwordHash, username);

        // Act
        await _repository.AddAsync(user);

        // Assert
        var savedUser = await _context.Users.FindAsync(12345L);
        savedUser.Should().NotBeNull();
        savedUser!.Username.Value.Should().Be("testuser");
        savedUser.Email.Value.Should().Be("test@example.com");
        savedUser.PasswordHash.Should().Be(passwordHash);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value;
        var username = Username.Create("testuser").Value;
        var passwordHash = "hashedpassword";
        var user = new User(12345L, email, passwordHash, username);
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(12345L);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(12345L);
        result.Username.Value.Should().Be("testuser");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenUserDoesNotExist()
    {
        // Act
        var result = await _repository.GetByIdAsync(99999L);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value;
        var username = Username.Create("testuser").Value;
        var passwordHash = "hashedpassword";
        var user = new User(12345L, email, passwordHash, username);
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByEmailAsync(email);

        // Assert
        result.Should().NotBeNull();
        result!.Email.Value.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnNull_WhenUserDoesNotExist()
    {
        // Arrange
        var email = Email.Create("nonexistent@example.com").Value;

        // Act
        var result = await _repository.GetByEmailAsync(email);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExistsByEmailAsync_ShouldReturnTrue_WhenUserExists()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value;
        var username = Username.Create("testuser").Value;
        var passwordHash = "hashedpassword";
        var user = new User(12345L, email, passwordHash, username);
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.ExistsByEmailAsync(email);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsByEmailAsync_ShouldReturnFalse_WhenUserDoesNotExist()
    {
        // Arrange
        var email = Email.Create("nonexistent@example.com").Value;

        // Act
        var result = await _repository.ExistsByEmailAsync(email);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateUserInDatabase()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value;
        var username = Username.Create("testuser").Value;
        var passwordHash = "hashedpassword";
        var user = new User(12345L, email, passwordHash, username);
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        user.UpdatePassword("newhashedpassword");
        await _repository.UpdateAsync(user);

        // Assert
        var updatedUser = await _context.Users.FindAsync(12345L);
        updatedUser.Should().NotBeNull();
        updatedUser!.PasswordHash.Should().Be("newhashedpassword");
    }
}