using MemberService.Domain.Entities;
using MemberService.Domain.Interfaces;
using MemberService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace MemberService.Infrastructure.Persistence.Repositories;

/// <summary>
/// User repository implementation
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly MemberDbContext _context;

    public UserRepository(MemberDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(long id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task<User?> GetByEmailAsync(Email email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<bool> ExistsByEmailAsync(Email email)
    {
        return await _context.Users
            .AnyAsync(u => u.Email == email);
    }

    public async Task<bool> ExistsByPhoneNumberAsync(string phoneNumber)
    {
        return await _context.Users
            .AnyAsync(u => u.PhoneNumber == phoneNumber);
    }

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }
}