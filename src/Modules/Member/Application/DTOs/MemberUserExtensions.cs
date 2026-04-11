using Member.Domain.Entities;

namespace Member.Application.DTOs;

public static class MemberUserExtensions
{
    public static UserDto ToDto(this MemberUser user)
    {
        return new UserDto(
            Id: user.Id,
            Email: user.Email,
            Username: user.Username,
            DisplayName: user.DisplayName,
            Role: user.Role.ToString(),
            PhoneCountryCodeId: user.PhoneCountryCodeId,
            PhoneDialCode: user.CountryCode?.DialCode ?? string.Empty,
            PhoneNumber: user.PhoneNumber,
            AddressCountry: user.AddressCountry,
            AddressCity: user.AddressCity,
            AddressPostalCode: user.AddressPostalCode,
            AddressLine: user.AddressLine,
            CreatedAt: user.CreatedAt);
    }
}
