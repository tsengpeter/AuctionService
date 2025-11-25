namespace MemberService.Domain.Interfaces;

/// <summary>
/// Interface for Snowflake ID generation.
/// Generates 64-bit, time-ordered, globally unique distributed IDs.
/// </summary>
public interface ISnowflakeIdGenerator
{
    /// <summary>
    /// Generates a new Snowflake ID.
    /// </summary>
    /// <returns>A new 64-bit Snowflake ID</returns>
    long GenerateId();
}
