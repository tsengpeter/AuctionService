using BiddingService.Core.Interfaces;
using IdGen;

namespace BiddingService.Infrastructure.IdGeneration;

public class SnowflakeIdGenerator : ISnowflakeIdGenerator
{
    private readonly IdGenerator _generator;

    public SnowflakeIdGenerator(int generatorId, int datacenterId)
    {
        _generator = new IdGenerator(generatorId);
    }

    public long GenerateId()
    {
        return _generator.CreateId();
    }
}