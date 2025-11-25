using MemberService.Application.DTOs.Users;

namespace MemberService.Application.Services;

/// <summary>
/// User service interface for user profile management operations
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Get the authenticated user's complete profile information
    /// </summary>
    /// <param name="userId">User ID from JWT claims</param>
    /// <returns>Complete user profile including email and timestamps</returns>
    Task<UserProfileResponse> GetMyProfileAsync(long userId);

    /// <summary>
    /// Get another user's public profile information
    /// </summary>
    /// <param name="userId">Target user ID to retrieve</param>
    /// <returns>Public profile with limited information (username and creation date only)</returns>
    /// <exception cref="UserNotFoundException">Thrown when user does not exist</exception>
    Task<PublicUserProfileResponse> GetUserProfileAsync(long userId);

    /// <summary>
    /// Update authenticated user's profile information
    /// </summary>
    /// <param name="userId">User ID from JWT claims</param>
    /// <param name="username">New username (optional, if null/empty will not update)</param>
    /// <param name="email">New email (optional, if null/empty will not update)</param>
    /// <returns>Updated user profile</returns>
    /// <exception cref="DuplicateEmailException">Thrown when new email already exists</exception>
    /// <exception cref="UserNotFoundException">Thrown when user does not exist</exception>
    Task<UserProfileResponse> UpdateProfileAsync(long userId, string? username, string? email);

    /// <summary>
    /// Change authenticated user's password
    /// </summary>
    /// <param name="userId">User ID from JWT claims</param>
    /// <param name="oldPassword">Current password for verification</param>
    /// <param name="newPassword">New password to set</param>
    /// <exception cref="InvalidPasswordException">Thrown when old password is incorrect</exception>
    /// <exception cref="UserNotFoundException">Thrown when user does not exist</exception>
    Task ChangePasswordAsync(long userId, string oldPassword, string newPassword);
}
