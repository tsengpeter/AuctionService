using IdGen;
using MemberService.Domain.Interfaces;

namespace MemberService.Infrastructure.IdGeneration;

/// <summary>
/// Snowflake ID generator implementation using IdGen library.
/// Generates 64-bit time-ordered, globally unique distributed IDs.
/// </summary>
public class SnowflakeIdGenerator : ISnowflakeIdGenerator
{
    private readonly IIdGenerator<long> _generator;

    public SnowflakeIdGenerator()
    {
        // Create IdGen with default settings
        // Machine ID and Datacenter ID default to 0 (can be configured if needed)
        var idStructure = new IdStructure(
            timestampBits: 41,      // ~69 years of millisecond precision
            generatorIdBits: 10,    // Worker + Datacenter combined (max 1024 generators)
            sequenceBits: 12        // Max 4096 IDs per millisecond
        );

        var options = new IdGeneratorOptions(idStructure);
        _generator = new IdGenerator(0, options); // Machine ID 0
    }

    /// <summary>
    /// Generates a new Snowflake ID.
    /// </summary>
    /// <returns>A new 64-bit time-ordered unique ID</returns>
    public long GenerateId()
    {
        return _generator.CreateId();
    }
}
