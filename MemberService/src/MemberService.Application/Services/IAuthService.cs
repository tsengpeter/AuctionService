using MemberService.Application.DTOs.Auth;

namespace MemberService.Application.Services;

public interface IAuthService
{
    Task<RegisterResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task LogoutAsync(string refreshToken);
    Task<TokenValidationResponse> ValidateTokenAsync(string token);
    Task<VerificationResponse> RequestEmailVerificationAsync(long userId, CancellationToken cancellationToken = default);
    Task<VerificationResponse> VerifyEmailAsync(long userId, string code, CancellationToken cancellationToken = default);
    Task<VerificationResponse> RequestPhoneVerificationAsync(long userId, CancellationToken cancellationToken = default);
    Task<VerificationResponse> VerifyPhoneAsync(long userId, string code, CancellationToken cancellationToken = default);
}