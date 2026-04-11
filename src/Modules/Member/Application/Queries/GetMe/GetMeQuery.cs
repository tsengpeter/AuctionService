using Member.Application.DTOs;
using MediatR;

namespace Member.Application.Queries.GetMe;

public record GetMeQuery(Guid UserId) : IRequest<GetMeQueryResult>;

public record GetMeQueryResult(UserDto User);
