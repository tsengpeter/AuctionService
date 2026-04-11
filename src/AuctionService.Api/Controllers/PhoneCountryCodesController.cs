using AuctionService.Shared;
using MediatR;
using Member.Application.Queries.GetPhoneCountryCodes;
using Microsoft.AspNetCore.Mvc;

namespace AuctionService.Api.Controllers;

/// <summary>Phone country code lookup (no JWT required)</summary>
[ApiController]
[Route("api/phone-country-codes")]
public class PhoneCountryCodesController : ControllerBase
{
    private readonly IMediator _mediator;

    public PhoneCountryCodesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Get all supported phone country codes</summary>
    /// <response code="200">List of country codes</response>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPhoneCountryCodesQuery(), cancellationToken);
        return Ok(ApiResponseFactory.Ok(result.PhoneCodes));
    }
}
