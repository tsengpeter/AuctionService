using System.Text.Json.Serialization;

namespace MemberService.Application.DTOs.Auth;

public record TokenValidationResponse(
    bool IsValid,
    long? UserId = null,
    DateTime? ExpiresAt = null
);