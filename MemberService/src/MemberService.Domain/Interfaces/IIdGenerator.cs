namespace MemberService.Domain.Interfaces;

/// <summary>
/// ID generator interface
/// </summary>
public interface IIdGenerator
{
    long GenerateId();
}