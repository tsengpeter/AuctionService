namespace MemberService.Application.DTOs.Auth;

public record UserInfo(
    long Id,
    string Email,
    string Username,
    DateTime CreatedAt
);