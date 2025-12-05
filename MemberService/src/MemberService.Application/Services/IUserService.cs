using MemberService.Application.DTOs.Users;

namespace MemberService.Application.Services;

public interface IUserService
{
    Task<UserProfileResponse> GetCurrentUserAsync(long userId);
    Task<UserPublicProfileResponse> GetUserByIdAsync(long userId);
}