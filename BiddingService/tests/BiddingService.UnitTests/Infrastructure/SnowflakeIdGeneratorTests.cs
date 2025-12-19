using BiddingService.Infrastructure.IdGeneration;
using FluentAssertions;
using Xunit;

namespace BiddingService.UnitTests.Infrastructure;

public class SnowflakeIdGeneratorTests
{
    [Fact]
    public void GenerateId_ShouldReturnPositiveLong()
    {
        // Arrange
        var generator = new SnowflakeIdGenerator(1, 1);

        // Act
        var id = generator.GenerateId();

        // Assert
        id.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GenerateId_ShouldReturnUniqueIds()
    {
        // Arrange
        var generator = new SnowflakeIdGenerator(1, 1);
        var ids = new HashSet<long>();

        // Act
        for (int i = 0; i < 1000; i++)
        {
            var id = generator.GenerateId();
            ids.Add(id);
        }

        // Assert
        ids.Count.Should().Be(1000, "All generated IDs should be unique");
    }

    [Fact]
    public void GenerateId_MultipleInstances_ShouldReturnUniqueIds()
    {
        // Arrange
        var generator1 = new SnowflakeIdGenerator(1, 1);
        var generator2 = new SnowflakeIdGenerator(2, 1);
        var ids = new HashSet<long>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            ids.Add(generator1.GenerateId());
            ids.Add(generator2.GenerateId());
        }

        // Assert
        ids.Count.Should().Be(200, "IDs from different generators should be unique");
    }

    [Fact]
    public void GenerateId_ShouldBeWithinReasonableRange()
    {
        // Arrange
        var generator = new SnowflakeIdGenerator(1, 1);

        // Act
        var id = generator.GenerateId();

        // Assert
        // Snowflake IDs are typically 64-bit with timestamp, worker, and sequence components
        // For a reasonable test, ensure it's a large positive number
        id.Should().BeGreaterThan(1000000000000000L); // Roughly > 2^50
        id.Should().BeLessThan(long.MaxValue);
    }

    [Fact]
    public void Constructor_ShouldAcceptValidGeneratorId()
    {
        // Arrange & Act
        var generator = new SnowflakeIdGenerator(1023, 31); // Max valid values

        // Assert
        generator.Should().NotBeNull();
    }

    [Fact]
    public void GenerateId_ShouldBeThreadSafe()
    {
        // Arrange
        var generator = new SnowflakeIdGenerator(1, 1);
        var ids = new System.Collections.Concurrent.ConcurrentBag<long>();

        // Act
        Parallel.For(0, 100, _ =>
        {
            ids.Add(generator.GenerateId());
        });

        // Assert
        ids.Distinct().Count().Should().Be(100, "All IDs should be unique even in parallel execution");
    }
}