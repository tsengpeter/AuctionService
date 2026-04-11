using Member.Application.DTOs;
using MediatR;

namespace Member.Application.Commands.Login;

public record LoginCommand(
    string Email,
    string Password,
    string? ClientIp) : IRequest<LoginCommandResult>;

public record LoginCommandResult(TokenDto Tokens);
