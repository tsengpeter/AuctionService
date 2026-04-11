using System.Security.Claims;
using AuctionService.Api.Controllers.Models;
using AuctionService.Shared;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Member.Application.Commands.ChangePassword;
using Member.Application.Commands.UpdateProfile;
using Member.Application.Queries.GetMe;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuctionService.Api.Controllers;

/// <summary>User profile endpoints (requires Bearer JWT)</summary>
[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Get the current user's profile</summary>
    /// <response code="200">User profile</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized(ApiResponseFactory.Fail("Unauthorized.", 401));

        try
        {
            var result = await _mediator.Send(new GetMeQuery(userId.Value), cancellationToken);
            return Ok(ApiResponseFactory.Ok(result.User));
        }
        catch (InvalidOperationException ex) when (ex.Message == "USER_NOT_FOUND")
        {
            return NotFound(ApiResponseFactory.Fail("User not found.", 404));
        }
    }

    /// <summary>Update the current user's profile</summary>
    /// <param name="request">Profile fields to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="200">Updated user profile</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="409">Username already taken</response>
    /// <response code="422">Validation failed</response>
    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized(ApiResponseFactory.Fail("Unauthorized.", 401));

        // Reject attempts to change phone/email via this endpoint
        if (request.Phone != null || request.Email != null)
        {
            throw new ValidationException(
                [new ValidationFailure("phone/email", "Phone and email cannot be changed via this endpoint.")]);
        }

        try
        {
            var command = new UpdateProfileCommand(
                UserId: userId.Value,
                Username: request.Username,
                DisplayName: request.DisplayName,
                AddressCountry: request.Address?.Country,
                AddressCity: request.Address?.City,
                AddressPostalCode: request.Address?.PostalCode,
                AddressLine: request.Address?.AddressLine);

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(ApiResponseFactory.Ok(result.User));
        }
        catch (InvalidOperationException ex) when (ex.Message == "USERNAME_CONFLICT")
        {
            return Conflict(ApiResponseFactory.Fail("Username already taken.", 409));
        }
    }

    /// <summary>Change the current user's password</summary>
    /// <param name="request">Current and new password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="204">Password changed</response>
    /// <response code="401">Wrong current password or unauthorized</response>
    /// <response code="422">New password fails complexity rules or matches old</response>
    [HttpPut("me/password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized(ApiResponseFactory.Fail("Unauthorized.", 401));

        try
        {
            var command = new ChangePasswordCommand(userId.Value, request.CurrentPassword, request.NewPassword);
            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message == "INVALID_CURRENT_PASSWORD")
        {
            return Unauthorized(ApiResponseFactory.Fail("Current password is incorrect.", 401));
        }
    }

    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
