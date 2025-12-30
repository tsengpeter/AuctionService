using MemberService.Domain.Exceptions;
using Xunit;

namespace MemberService.Domain.Tests.Exceptions;

public class DomainExceptionTests
{
    [Fact]
    public void DomainException_WithMessage_CreatesInstance()
    {
        // Arrange
        var message = "Test domain error";

        // Act
        var exception = new TestDomainException(message);

        // Assert
        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void DomainException_WithMessageAndInnerException_CreatesInstance()
    {
        // Arrange
        var message = "Test domain error";
        var innerException = new Exception("Inner error");

        // Act
        var exception = new TestDomainException(message, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }

    // Test implementation of DomainException
    private class TestDomainException : DomainException
    {
        public TestDomainException(string message) : base(message)
        {
        }

        public TestDomainException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}