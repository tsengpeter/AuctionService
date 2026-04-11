using Member.Application.DTOs;
using MediatR;

namespace Member.Application.Commands.UpdateProfile;

public record UpdateProfileCommand(
    Guid UserId,
    string Username,
    string? DisplayName,
    string? AddressCountry,
    string? AddressCity,
    string? AddressPostalCode,
    string? AddressLine) : IRequest<UpdateProfileCommandResult>;

public record UpdateProfileCommandResult(UserDto User);
