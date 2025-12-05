using MemberService.Application.DTOs.Users;
using MemberService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MemberService.API.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetMe()
    {
        // In a real implementation, this would come from JWT token claims
        // For now, we'll use a placeholder user ID
        var userId = GetCurrentUserId();
        var profile = await _userService.GetCurrentUserAsync(userId);
        return Ok(profile);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(UserPublicProfileResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetUserById(long id)
    {
        var profile = await _userService.GetUserByIdAsync(id);
        return Ok(profile);
    }

    [Authorize]
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserProfileResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = GetCurrentUserId();
        var updatedProfile = await _userService.UpdateProfileAsync(userId, request);
        return Ok(updatedProfile);
    }

    [Authorize]
    [HttpPut("me/password")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = GetCurrentUserId();
        await _userService.ChangePasswordAsync(userId, request);
        return NoContent();
    }

    private long GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid or missing user ID in token");
        }
        return userId;
    }
}