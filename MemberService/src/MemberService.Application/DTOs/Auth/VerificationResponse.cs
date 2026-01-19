namespace MemberService.Application.DTOs.Auth;

/// <summary>
/// Response DTO for verification operations
/// </summary>
public class VerificationResponse
{
    /// <summary>
    /// Whether verification operation succeeded
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Response message
    /// </summary>
    public string Message { get; set; }

    public VerificationResponse(bool success, string message)
    {
        Success = success;
        Message = message;
    }
}
