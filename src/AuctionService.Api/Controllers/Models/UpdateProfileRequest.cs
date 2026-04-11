namespace AuctionService.Api.Controllers.Models;

public record UpdateProfileRequest(
    string Username,
    string? DisplayName,
    AddressInput? Address,
    string? Phone,
    string? Email);
