using Xunit;
using FluentAssertions;
using MemberService.Domain.ValueObjects;

namespace MemberService.Domain.Tests.ValueObjects;

public class PasswordTests
{
    [Fact]
    public void Create_WithValidPassword_ReturnsPasswordValueObject()
    {
        // Arrange
        var password = "ValidPassword123!";
        
        // Act
        var result = Password.Create(password);
        
        // Assert
        result.Value.Should().Be("ValidPassword123!");
    }
    
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyPassword_ThrowsArgumentException(string password)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Password.Create(password));
    }
    
    [Fact]
    public void Create_WithPasswordTooShort_ThrowsArgumentException()
    {
        // Arrange
        var password = "Short1!"; // 7 characters, minimum is 8
        
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Password.Create(password));
    }
    
    [Fact]
    public void Create_WithPasswordTooLong_ThrowsArgumentException()
    {
        // Arrange
        var password = new string('a', 129); // 129 characters, maximum is 128
        
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Password.Create(password));
    }
    
    [Fact]
    public void Create_WithExactlyMinimumLength_ReturnsPasswordValueObject()
    {
        // Arrange
        var password = "Pass1234"; // 8 characters
        
        // Act
        var result = Password.Create(password);
        
        // Assert
        result.Value.Should().Be("Pass1234");
    }
    
    [Fact]
    public void Create_WithExactlyMaximumLength_ReturnsPasswordValueObject()
    {
        // Arrange
        var password = new string('a', 128); // 128 characters
        
        // Act
        var result = Password.Create(password);
        
        // Assert
        result.Value.Should().HaveLength(128);
    }
    
    [Fact]
    public void EqualityOperator_WithSamePassword_ReturnsTrue()
    {
        // Arrange
        var password1 = Password.Create("MyPassword123!");
        var password2 = Password.Create("MyPassword123!");
        
        // Act & Assert
        (password1 == password2).Should().BeTrue();
    }
    
    [Fact]
    public void EqualityOperator_WithDifferentPassword_ReturnsFalse()
    {
        // Arrange
        var password1 = Password.Create("MyPassword123!");
        var password2 = Password.Create("DifferentPass123!");
        
        // Act & Assert
        (password1 == password2).Should().BeFalse();
    }
}
