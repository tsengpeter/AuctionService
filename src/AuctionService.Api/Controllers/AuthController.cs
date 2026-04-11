using AuctionService.Shared;
using MediatR;
using Member.Application.Commands.Login;
using Member.Application.Commands.Logout;
using Member.Application.Commands.RefreshToken;
using Member.Application.Commands.Register;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AuctionService.Api.Controllers;

/// <summary>Authentication endpoints (no JWT required)</summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Register a new user account</summary>
    /// <param name="request">Registration details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="201">Account created</response>
    /// <response code="409">Email already registered</response>
    /// <response code="422">Validation failed</response>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var command = new RegisterCommand(
                Email: request.Email,
                Username: request.Username,
                Password: request.Password,
                DisplayName: request.DisplayName,
                PhoneCountryCodeId: request.PhoneCountryCodeId,
                PhoneNumber: request.PhoneNumber,
                AddressCountry: request.Address?.Country,
                AddressCity: request.Address?.City,
                AddressPostalCode: request.Address?.PostalCode,
                AddressLine: request.Address?.AddressLine);

            var result = await _mediator.Send(command, cancellationToken);
            return StatusCode(201, ApiResponseFactory.Ok(result.User, 201));
        }
        catch (InvalidOperationException ex) when (ex.Message == "EMAIL_CONFLICT")
        {
            return Conflict(ApiResponseFactory.Fail("Email already registered.", 409));
        }
    }

    /// <summary>Login and receive access + refresh tokens</summary>
    /// <param name="request">Login credentials</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="200">Login successful, tokens returned</response>
    /// <response code="401">Invalid credentials</response>
    /// <response code="429">Too many login attempts</response>
    [HttpPost("login")]
    [EnableRateLimiting("login-ip")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            var command = new LoginCommand(
                Email: request.Email,
                Password: request.Password,
                ClientIp: clientIp);

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(ApiResponseFactory.Ok(result.Tokens));
        }
        catch (InvalidOperationException ex) when (ex.Message == "INVALID_CREDENTIALS")
        {
            return Unauthorized(ApiResponseFactory.Fail("Invalid email or password.", 401));
        }
        catch (InvalidOperationException ex) when (ex.Message == "TOO_MANY_ATTEMPTS")
        {
            return StatusCode(429, ApiResponseFactory.Fail("Too many login attempts. Please try again later.", 429));
        }
    }

    /// <summary>Refresh access token using a refresh token</summary>
    /// <param name="request">Refresh token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="200">New tokens returned, old refresh token revoked</response>
    /// <response code="401">Invalid, expired, or revoked token</response>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var command = new RefreshTokenCommand(request.RefreshToken);
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(ApiResponseFactory.Ok(result.Tokens));
        }
        catch (InvalidOperationException ex) when (ex.Message == "INVALID_REFRESH_TOKEN")
        {
            return Unauthorized(ApiResponseFactory.Fail("Invalid or expired refresh token.", 401));
        }
    }

    /// <summary>Logout and revoke a refresh token</summary>
    /// <param name="request">Refresh token to revoke</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="204">Logged out (idempotent)</response>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken cancellationToken)
    {
        var command = new LogoutCommand(request.RefreshToken);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}

public record RegisterRequest(
    string Email,
    string Username,
    string Password,
    string? DisplayName,
    int PhoneCountryCodeId,
    string PhoneNumber,
    AddressInput? Address);

public record AddressInput(
    string? Country,
    string? City,
    string? PostalCode,
    string? AddressLine);

public record LoginRequest(string Email, string Password);
public record RefreshTokenRequest(string RefreshToken);
public record LogoutRequest(string RefreshToken);
