using Member.Application.DTOs;
using Member.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Member.Application.Queries.GetPhoneCountryCodes;

public class GetPhoneCountryCodesQueryHandler : IRequestHandler<GetPhoneCountryCodesQuery, GetPhoneCountryCodesQueryResult>
{
    private readonly MemberDbContext _db;

    public GetPhoneCountryCodesQueryHandler(MemberDbContext db)
    {
        _db = db;
    }

    public async Task<GetPhoneCountryCodesQueryResult> Handle(GetPhoneCountryCodesQuery query, CancellationToken cancellationToken)
    {
        var codes = await _db.PhoneCountryCodes
            .Select(p => new PhoneCountryCodeDto(p.Id, p.DialCode, p.CountryName, p.CountryIso))
            .ToArrayAsync(cancellationToken);

        return new GetPhoneCountryCodesQueryResult(codes);
    }
}
