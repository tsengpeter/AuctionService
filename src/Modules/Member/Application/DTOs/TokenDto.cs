namespace Member.Application.DTOs;

public record TokenDto(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn);
