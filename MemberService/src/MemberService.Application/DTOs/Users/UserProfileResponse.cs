namespace MemberService.Application.DTOs.Users;

public record UserProfileResponse(
    long Id,
    string Email,
    string Username,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);