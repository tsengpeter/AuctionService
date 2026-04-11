using Member.Application.DTOs;
using MediatR;

namespace Member.Application.Queries.GetPhoneCountryCodes;

public record GetPhoneCountryCodesQuery : IRequest<GetPhoneCountryCodesQueryResult>;

public record GetPhoneCountryCodesQueryResult(PhoneCountryCodeDto[] PhoneCodes);
