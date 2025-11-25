using MemberService.Application.DTOs.Users;
using MemberService.Domain.Entities;
using MemberService.Domain.Exceptions;
using MemberService.Domain.Interfaces;

namespace MemberService.Application.Services;

/// <summary>
/// Service for user profile management operations
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
    }

    /// <summary>
    /// Get authenticated user's complete profile including email
    /// </summary>
    public async Task<UserProfileResponse> GetMyProfileAsync(long userId)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new UserNotFoundException($"User with ID {userId} not found");

        return new UserProfileResponse
        {
            UserId = user.Id,
            Email = user.Email.Value,
            Username = user.Username.Value,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    /// <summary>
    /// Get public profile for another user (limited information)
    /// </summary>
    public async Task<PublicUserProfileResponse> GetUserProfileAsync(long userId)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new UserNotFoundException($"User with ID {userId} not found");

        return new PublicUserProfileResponse
        {
            UserId = user.Id,
            Username = user.Username.Value,
            CreatedAt = user.CreatedAt
        };
    }

    /// <summary>
    /// Update user's profile information (username and/or email)
    /// Validates email uniqueness if email is being changed
    /// </summary>
    public async Task<UserProfileResponse> UpdateProfileAsync(long userId, string? username, string? email)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new UserNotFoundException($"User with ID {userId} not found");

        // Check email uniqueness if updating email
        if (!string.IsNullOrWhiteSpace(email) && email != user.Email.Value)
        {
            var existingUser = await _userRepository.FindByEmailAsync(email);
            if (existingUser != null)
            {
                throw new DuplicateEmailException($"Email '{email}' is already in use");
            }
            user.UpdateEmail(MemberService.Domain.ValueObjects.Email.Create(email));
        }

        // Update username if provided
        if (!string.IsNullOrWhiteSpace(username) && username != user.Username.Value)
        {
            user.UpdateUsername(MemberService.Domain.ValueObjects.Username.Create(username));
        }

        await _userRepository.UpdateAsync(user);

        return new UserProfileResponse
        {
            UserId = user.Id,
            Email = user.Email.Value,
            Username = user.Username.Value,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    /// <summary>
    /// Change user's password
    /// Verifies old password and revokes all existing refresh tokens
    /// </summary>
    public async Task ChangePasswordAsync(long userId, string oldPassword, string newPassword)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new UserNotFoundException($"User with ID {userId} not found");

        // Verify old password
        if (!_passwordHasher.VerifyPassword(oldPassword, user.PasswordHash))
        {
            throw new InvalidPasswordException("Current password is incorrect");
        }

        // Update password with new hash
        var newPasswordHash = _passwordHasher.HashPassword(newPassword, userId);
        user.UpdatePassword(newPasswordHash);

        // Revoke all refresh tokens for this user (security best practice)
        await _refreshTokenRepository.RevokeAllForUserAsync(userId);

        // Save user with new password
        await _userRepository.UpdateAsync(user);
    }
}
