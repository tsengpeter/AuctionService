using MediatR;

namespace Member.Application.Commands.Logout;

public record LogoutCommand(string RawToken) : IRequest;
