using AuctionService.UnitTests.Member;
using FluentAssertions;
using Member.Application.Queries.GetPhoneCountryCodes;
using Member.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.UnitTests.Member.Application;

public class GetPhoneCountryCodesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnAll_PhoneCountryCodes()
    {
        using var db = MemberDbContextFactory.CreateWithPhoneCountryCodes();
        var handler = new GetPhoneCountryCodesQueryHandler(db);

        var result = await handler.Handle(new GetPhoneCountryCodesQuery(), CancellationToken.None);

        result.PhoneCodes.Should().NotBeEmpty();
        result.PhoneCodes.Should().Contain(p => p.DialCode == "886");
    }

    [Fact]
    public async Task Handle_EmptyDb_ShouldReturnEmptyArray()
    {
        var options = new DbContextOptionsBuilder<MemberDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new MemberDbContext(options);

        var handler = new GetPhoneCountryCodesQueryHandler(db);
        var result = await handler.Handle(new GetPhoneCountryCodesQuery(), CancellationToken.None);

        result.PhoneCodes.Should().BeEmpty();
    }
}
