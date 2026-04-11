namespace Member.Application.DTOs;

public record UserDto(
    Guid Id,
    string Email,
    string Username,
    string? DisplayName,
    string Role,
    int PhoneCountryCodeId,
    string PhoneDialCode,
    string PhoneNumber,
    string? AddressCountry,
    string? AddressCity,
    string? AddressPostalCode,
    string? AddressLine,
    DateTimeOffset CreatedAt);
