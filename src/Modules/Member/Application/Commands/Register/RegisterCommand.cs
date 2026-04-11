using Member.Application.DTOs;
using MediatR;

namespace Member.Application.Commands.Register;

public record RegisterCommand(
    string Email,
    string Username,
    string Password,
    string? DisplayName,
    int PhoneCountryCodeId,
    string PhoneNumber,
    string? AddressCountry,
    string? AddressCity,
    string? AddressPostalCode,
    string? AddressLine) : IRequest<RegisterCommandResult>;

public record RegisterCommandResult(UserDto User);
