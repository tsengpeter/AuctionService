using Xunit;
using FluentAssertions;
using MemberService.Infrastructure.IdGeneration;

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
    public void GenerateId_ProducesUniqueIds()
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
    public void GenerateId_IdsAreTimeOrdered()
    {
        // Arrange
        var generator = new SnowflakeIdGenerator();
        var id1 = generator.GenerateId();

        // Wait a bit to ensure time progression
        System.Threading.Thread.Sleep(1);

        var id2 = generator.GenerateId();

        // Assert
        id2.Should().BeGreaterThan(id1);
    }

    [Fact]
    public void GenerateId_MultipleCalls_AreConsistent()
    {
        // Arrange
        var generator = new SnowflakeIdGenerator();

        // Act
        var ids = Enumerable.Range(0, 100).Select(_ => generator.GenerateId()).ToList();

        // Assert
        ids.Should().AllSatisfy(id => id.Should().BeGreaterThan(0));
    }
}
