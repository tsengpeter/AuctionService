using BiddingService.Infrastructure.Encryption;
using FluentAssertions;
using System.Security.Cryptography;
using Xunit;

namespace BiddingService.UnitTests.Infrastructure;

public class EncryptionServiceTests
{
    private const string TestKey = "0123456789abcdef0123456789abcdef"; // 32 bytes for AES-256
    private const string TestIv = "0123456789abcdef"; // 16 bytes for AES
    private readonly EncryptionService _encryptionService;

    public EncryptionServiceTests()
    {
        _encryptionService = new EncryptionService(TestKey, TestIv);
    }

    [Fact]
    public void Encrypt_WhenCalledWithPlainText_ShouldReturnNonEmptyString()
    {
        // Arrange
        var plainText = "Hello, World!";

        // Act
        var encrypted = _encryptionService.Encrypt(plainText);

        // Assert
        encrypted.Should().NotBeNullOrEmpty();
        encrypted.Should().NotBe(plainText);
    }

    [Fact]
    public void EncryptDecrypt_WhenRoundTrip_ShouldReturnOriginalText()
    {
        // Arrange
        var originalText = "This is a test message for encryption.";

        // Act
        var encrypted = _encryptionService.Encrypt(originalText);
        var decrypted = _encryptionService.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(originalText);
    }

    [Fact]
    public void Encrypt_WhenCalledMultipleTimesWithSameInput_ShouldReturnSameResults()
    {
        // Arrange
        var plainText = "Same input text";

        // Act
        var encrypted1 = _encryptionService.Encrypt(plainText);
        var encrypted2 = _encryptionService.Encrypt(plainText);

        // Assert
        encrypted1.Should().Be(encrypted2); // AES is deterministic with same key/IV
    }

    [Fact]
    public void Decrypt_WhenCalledWithValidCipherText_ShouldReturnPlainText()
    {
        // Arrange
        var plainText = "Test decryption";
        var cipherText = _encryptionService.Encrypt(plainText);

        // Act
        var decrypted = _encryptionService.Decrypt(cipherText);

        // Assert
        decrypted.Should().Be(plainText);
    }

    [Fact]
    public void Decrypt_WhenCalledWithInvalidBase64_ShouldThrowException()
    {
        // Arrange
        var invalidCipherText = "invalid-base64-string!";

        // Act
        Action act = () => _encryptionService.Decrypt(invalidCipherText);

        // Assert
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void Decrypt_WhenCalledWithWrongKey_ShouldThrowException()
    {
        // Arrange
        var plainText = "Test with wrong key";
        var wrongKeyService = new EncryptionService("wrongkey123456789012345678901234567890", TestIv);
        var cipherText = _encryptionService.Encrypt(plainText);

        // Act
        Action act = () => wrongKeyService.Decrypt(cipherText);

        // Assert
        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void Encrypt_WhenCalledWithEmptyString_ShouldWork()
    {
        // Arrange
        var emptyText = string.Empty;

        // Act
        var encrypted = _encryptionService.Encrypt(emptyText);
        var decrypted = _encryptionService.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(emptyText);
    }

    [Fact]
    public void Encrypt_WhenCalledWithUnicodeText_ShouldWork()
    {
        // Arrange
        var unicodeText = "Hello 世界 🌍 Test with émojis 🎉";

        // Act
        var encrypted = _encryptionService.Encrypt(unicodeText);
        var decrypted = _encryptionService.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(unicodeText);
    }

    [Fact]
    public void Encrypt_WhenCalledWithLongText_ShouldWork()
    {
        // Arrange
        var longText = new string('A', 10000);

        // Act
        var encrypted = _encryptionService.Encrypt(longText);
        var decrypted = _encryptionService.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(longText);
    }

    [Fact]
    public void Constructor_WhenCalledWithValidKeyAndIv_ShouldNotThrow()
    {
        // Arrange & Act
        Action act = () => new EncryptionService(TestKey, TestIv);

        // Assert
        act.Should().NotThrow();
    }
}