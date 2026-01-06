namespace MemberService.Application.DTOs.Auth;

public record RegisterResponse(
    UserInfo User,
    string Message = "Registration successful. Please login to continue."
);
