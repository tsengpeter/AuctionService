namespace MemberService.Application.DTOs.Auth;

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserInfo User,
    string TokenType = "Bearer"
);