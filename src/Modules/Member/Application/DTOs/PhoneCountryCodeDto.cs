namespace Member.Application.DTOs;

public record PhoneCountryCodeDto(
    int Id,
    string DialCode,
    string CountryName,
    string CountryIso);
