using Member.Application.DTOs;
using MediatR;

namespace Member.Application.Commands.RefreshToken;

public record RefreshTokenCommand(string RawToken) : IRequest<RefreshTokenCommandResult>;

public record RefreshTokenCommandResult(TokenDto Tokens);
