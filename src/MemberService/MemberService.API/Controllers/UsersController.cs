using MemberService.Application.DTOs.Users;
using MemberService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MemberService.API.Controllers;

/// <summary>
/// Users controller for user profile management operations
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Get authenticated user's complete profile
    /// </summary>
    /// <returns>User's full profile including email</returns>
    /// <response code="200">User profile retrieved successfully</response>
    /// <response code="401">Unauthorized - JWT token required</response>
    [HttpGet("me")]
    public async Task<ActionResult<UserProfileResponse>> GetMyProfile()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized("Invalid or missing user ID in token");
        }

        var profile = await _userService.GetMyProfileAsync(userId);
        return Ok(profile);
    }

    /// <summary>
    /// Get another user's public profile information
    /// </summary>
    /// <param name="id">User ID to retrieve</param>
    /// <returns>Public profile with limited information (username and creation date)</returns>
    /// <response code="200">Public profile retrieved successfully</response>
    /// <response code="401">Unauthorized - JWT token required</response>
    /// <response code="404">User not found</response>
    [HttpGet("{id}")]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any, NoStore = false)]
    public async Task<ActionResult<PublicUserProfileResponse>> GetUserProfile(long id)
    {
        var profile = await _userService.GetUserProfileAsync(id);
        return Ok(profile);
    }

    /// <summary>
    /// Update authenticated user's profile (username and/or email)
    /// </summary>
    /// <param name="request">Update profile request with optional username and email</param>
    /// <returns>Updated user profile</returns>
    /// <response code="200">Profile updated successfully</response>
    /// <response code="400">Invalid request data or duplicate email</response>
    /// <response code="401">Unauthorized - JWT token required</response>
    [HttpPut("me")]
    public async Task<ActionResult<UserProfileResponse>> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized("Invalid or missing user ID in token");
        }

        var profile = await _userService.UpdateProfileAsync(userId, request.Username, request.Email);
        return Ok(profile);
    }

    /// <summary>
    /// Change authenticated user's password
    /// Validates old password and revokes all existing refresh tokens for security
    /// </summary>
    /// <param name="request">Password change request with old and new password</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Password changed successfully</response>
    /// <response code="400">Invalid old password or validation error</response>
    /// <response code="401">Unauthorized - JWT token required</response>
    [HttpPut("me/password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized("Invalid or missing user ID in token");
        }

        await _userService.ChangePasswordAsync(userId, request.OldPassword, request.NewPassword);
        return NoContent();
    }
}
