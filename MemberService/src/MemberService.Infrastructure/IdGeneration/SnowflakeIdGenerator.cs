using IdGen;
using MemberService.Domain.Interfaces;

namespace MemberService.Infrastructure.IdGeneration;

/// <summary>
/// Snowflake ID generator implementation
/// </summary>
public class SnowflakeIdGenerator : IIdGenerator
{
    private readonly IdGenerator _idGenerator;

    public SnowflakeIdGenerator(int workerId = 0, int datacenterId = 0)
    {
        // IdGen uses a single generatorId, combining worker and datacenter
        // For simplicity, use workerId as generatorId
        var generatorId = new IdGenerator(workerId);
        _idGenerator = generatorId;
    }

    public long GenerateId()
    {
        return _idGenerator.CreateId();
    }
}