namespace MemberService.Application.DTOs.Users;

public record UserPublicProfileResponse(
    long Id,
    string Username,
    DateTime CreatedAt
);