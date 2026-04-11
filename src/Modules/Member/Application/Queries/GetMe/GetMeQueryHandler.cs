using Member.Application.DTOs;
using Member.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Member.Application.Queries.GetMe;

public class GetMeQueryHandler : IRequestHandler<GetMeQuery, GetMeQueryResult>
{
    private readonly MemberDbContext _db;
    private readonly ILogger<GetMeQueryHandler> _logger;

    public GetMeQueryHandler(MemberDbContext db, ILogger<GetMeQueryHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<GetMeQueryResult> Handle(GetMeQuery query, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .Include(u => u.CountryCode)
            .FirstOrDefaultAsync(u => u.Id == query.UserId, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("GetMe: user {UserId} not found", query.UserId);
            throw new InvalidOperationException("USER_NOT_FOUND");
        }

        return new GetMeQueryResult(user.ToDto());
    }
}
