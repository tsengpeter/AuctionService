using MemberService.Application.DTOs.Users;
using MemberService.Application.Services;
using MemberService.Domain.Exceptions;
using MemberService.Domain.Interfaces;

namespace MemberService.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
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
}