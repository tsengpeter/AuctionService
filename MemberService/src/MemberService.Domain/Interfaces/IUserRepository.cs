using MemberService.Domain.Entities;
using MemberService.Domain.ValueObjects;

namespace MemberService.Domain.Interfaces;

/// <summary>
/// User repository interface
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(long id);
    Task<User?> GetByEmailAsync(Email email);
    Task<bool> ExistsByEmailAsync(Email email);
    Task<bool> ExistsByPhoneNumberAsync(string phoneNumber);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
}