using Microsoft.EntityFrameworkCore;
using MemberService.Domain.Entities;
using MemberService.Domain.Exceptions;
using MemberService.Domain.Interfaces;

namespace MemberService.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for User entity.
/// Provides persistence operations for User aggregates.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly MemberDbContext _context;

    public UserRepository(MemberDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email.Value == email.ToLowerInvariant(), cancellationToken);
    }

    public async Task<User?> FindByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            return null;

        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var user = await FindByIdAsync(id, cancellationToken);
        if (user == null)
            throw new UserNotFoundException(id);

        return user;
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        // Check for duplicate email
        var existingUser = await FindByEmailAsync(user.Email.Value, cancellationToken);
        if (existingUser != null)
            throw new EmailAlreadyExistsException(user.Email.Value);

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        _context.Users.Update(user);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
