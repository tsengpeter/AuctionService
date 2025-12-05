using MemberService.Application.DTOs.Users;

namespace MemberService.Application.Services;

public interface IUserService
{
    Task<UserProfileResponse> GetCurrentUserAsync(long userId);
    Task<UserPublicProfileResponse> GetUserByIdAsync(long userId);
    Task<UserProfileResponse> UpdateProfileAsync(long userId, UpdateProfileRequest request);
    Task ChangePasswordAsync(long userId, ChangePasswordRequest request);
}