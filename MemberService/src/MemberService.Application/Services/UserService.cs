using MemberService.Application.DTOs.Users;
using MemberService.Application.Services;
using MemberService.Domain.Exceptions;
using MemberService.Domain.Interfaces;
using MemberService.Domain.ValueObjects;

namespace MemberService.Application.Services;

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

    public async Task<UserProfileResponse> GetCurrentUserAsync(long userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new UserNotFoundException(userId);
        }

        return new UserProfileResponse(
            Id: user.Id,
            Email: user.Email.Value,
            Username: user.Username.Value,
            CreatedAt: user.CreatedAt,
            UpdatedAt: user.UpdatedAt
        );
    }

    public async Task<UserPublicProfileResponse> GetUserByIdAsync(long userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new UserNotFoundException(userId);
        }

        return new UserPublicProfileResponse(
            Id: user.Id,
            Username: user.Username.Value,
            CreatedAt: user.CreatedAt
        );
    }

    public async Task<UserProfileResponse> UpdateProfileAsync(long userId, UpdateProfileRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new UserNotFoundException(userId);
        }

        // Update email if provided
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var emailResult = Email.Create(request.Email);
            if (!emailResult.IsSuccess)
            {
                throw new ArgumentException(emailResult.Error);
            }
            var newEmail = emailResult.Value!;

            // Check if email is already taken by another user
            if (await _userRepository.ExistsByEmailAsync(newEmail) && !user.Email.Equals(newEmail))
            {
                throw new EmailAlreadyExistsException(request.Email);
            }

            user.UpdateEmail(newEmail);
        }

        // Update username if provided
        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            var usernameResult = Username.Create(request.Username);
            if (!usernameResult.IsSuccess)
            {
                throw new ArgumentException(usernameResult.Error);
            }
            var newUsername = usernameResult.Value!;

            user.UpdateUsername(newUsername);
        }

        await _userRepository.UpdateAsync(user);

        return new UserProfileResponse(
            Id: user.Id,
            Email: user.Email.Value,
            Username: user.Username.Value,
            CreatedAt: user.CreatedAt,
            UpdatedAt: user.UpdatedAt
        );
    }

    public async Task ChangePasswordAsync(long userId, ChangePasswordRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new UserNotFoundException(userId);
        }

        // Verify old password
        if (!_passwordHasher.VerifyPassword(request.OldPassword, userId, user.PasswordHash))
        {
            throw new InvalidPasswordException();
        }

        // Hash new password
        var newPasswordHash = _passwordHasher.HashPassword(request.NewPassword, userId);
        user.UpdatePassword(newPasswordHash);

        await _userRepository.UpdateAsync(user);

        // Revoke all refresh tokens for security
        await _refreshTokenRepository.RevokeAllUserTokensAsync(userId);
    }
}