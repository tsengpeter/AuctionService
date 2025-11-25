using MemberService.Domain.Entities;

namespace MemberService.Domain.Interfaces;

/// <summary>
/// Repository interface for User entity operations.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Finds a user by email address.
    /// </summary>
    /// <param name="email">The email to search for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The user if found, otherwise null</returns>
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Finds a user by ID.
    /// </summary>
    /// <param name="id">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The user if found, otherwise null</returns>
    Task<User?> FindByIdAsync(long id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a user by ID, throwing an exception if not found.
    /// </summary>
    /// <param name="id">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The user</returns>
    /// <exception cref="DomainException">Thrown when user is not found</exception>
    Task<User> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds a new user to the repository.
    /// </summary>
    /// <param name="user">The user to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an existing user.
    /// </summary>
    /// <param name="user">The user to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
}
