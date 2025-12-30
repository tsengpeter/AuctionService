using MemberService.Infrastructure.IdGeneration;
using Xunit;
using FluentAssertions;

namespace MemberService.Infrastructure.Tests.IdGeneration;

public class SnowflakeIdGeneratorTests
{
    [Fact]
    public void GenerateId_ReturnsPositiveLong()
    {
        // Arrange
        var generator = new SnowflakeIdGenerator();

        // Act
        var id = generator.GenerateId();

        // Assert
        id.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GenerateId_ReturnsUniqueIds()
    {
        // Arrange
        var generator = new SnowflakeIdGenerator();
        var ids = new HashSet<long>();

        // Act
        for (int i = 0; i < 1000; i++)
        {
            ids.Add(generator.GenerateId());
        }

        // Assert
        ids.Count.Should().Be(1000);
    }

    [Fact]
    public void GenerateId_WithDifferentWorkerIds_ReturnsDifferentIds()
    {
        // Arrange
        var generator1 = new SnowflakeIdGenerator(0);
        var generator2 = new SnowflakeIdGenerator(1);

        // Act
        var id1 = generator1.GenerateId();
        var id2 = generator2.GenerateId();

        // Assert
        id1.Should().NotBe(id2);
    }

    [Fact]
    public void Constructor_WithCustomWorkerAndDatacenterIds_CreatesGenerator()
    {
        // Arrange & Act
        var generator = new SnowflakeIdGenerator(5, 3);

        // Assert
        generator.Should().NotBeNull();
        var id = generator.GenerateId();
        id.Should().BeGreaterThan(0);
    }
}