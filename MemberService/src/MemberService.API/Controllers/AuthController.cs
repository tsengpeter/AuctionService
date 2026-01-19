using MemberService.Application.DTOs.Auth;
using MemberService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MemberService.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var response = await _authService.RegisterAsync(request);
        return Created(string.Empty, response);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var response = await _authService.LoginAsync(request);
        return Ok(response);
    }

    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var response = await _authService.RefreshTokenAsync(request);
        return Ok(response);
    }

    [HttpPost("logout")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        await _authService.LogoutAsync(request.RefreshToken);
        return NoContent();
    }

    [HttpGet("validate")]
    [ProducesResponseType(typeof(TokenValidationResponse), 200)]
    public async Task<IActionResult> Validate([FromQuery] string? token)
    {
        // Check if token parameter is provided
        if (string.IsNullOrWhiteSpace(token))
        {
            return Ok(new TokenValidationResponse(IsValid: false, ErrorMessage: "Token parameter is required"));
        }

        var response = await _authService.ValidateTokenAsync(token);

        // Always return 200 OK with TokenValidationResponse
        return Ok(response);
    }

    [HttpPost("request-email-verification")]
    [Authorize]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> RequestEmailVerification()
    {
        try
        {
            var userId = long.Parse(User.FindFirst("sub")?.Value ?? throw new UnauthorizedAccessException());
            var response = await _authService.RequestEmailVerificationAsync(userId);
            return Ok(response);
        }
        catch (Domain.Exceptions.VerificationCodeCooldownException ex)
        {
            return BadRequest(new
            {
                success = false,
                error = new
                {
                    code = "VERIFICATION_CODE_COOLDOWN",
                    message = ex.Message,
                    remainingSeconds = ex.RemainingSeconds
                }
            });
        }
    }

    [HttpPost("verify-email")]
    [Authorize]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        try
        {
            var userId = long.Parse(User.FindFirst("sub")?.Value ?? throw new UnauthorizedAccessException());
            var response = await _authService.VerifyEmailAsync(userId, request.Code);
            return Ok(response);
        }
        catch (Domain.Exceptions.InvalidVerificationCodeException ex)
        {
            return BadRequest(new
            {
                success = false,
                error = new
                {
                    code = "INVALID_VERIFICATION_CODE",
                    message = ex.Message
                }
            });
        }
    }

    [HttpPost("request-phone-verification")]
    [Authorize]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> RequestPhoneVerification()
    {
        try
        {
            var userId = long.Parse(User.FindFirst("sub")?.Value ?? throw new UnauthorizedAccessException());
            var response = await _authService.RequestPhoneVerificationAsync(userId);
            return Ok(response);
        }
        catch (Domain.Exceptions.VerificationCodeCooldownException ex)
        {
            return BadRequest(new
            {
                success = false,
                error = new
                {
                    code = "VERIFICATION_CODE_COOLDOWN",
                    message = ex.Message,
                    remainingSeconds = ex.RemainingSeconds
                }
            });
        }
    }

    [HttpPost("verify-phone")]
    [Authorize]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> VerifyPhone([FromBody] VerifyPhoneRequest request)
    {
        try
        {
            var userId = long.Parse(User.FindFirst("sub")?.Value ?? throw new UnauthorizedAccessException());
            var response = await _authService.VerifyPhoneAsync(userId, request.Code);
            return Ok(response);
        }
        catch (Domain.Exceptions.InvalidVerificationCodeException ex)
        {
            return BadRequest(new
            {
                success = false,
                error = new
                {
                    code = "INVALID_VERIFICATION_CODE",
                    message = ex.Message
                }
            });
        }
    }
}