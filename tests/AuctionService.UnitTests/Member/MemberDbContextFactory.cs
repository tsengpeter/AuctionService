using Member.Domain;
using Member.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.UnitTests.Member;

internal static class MemberDbContextFactory
{
    public static MemberDbContext CreateWithPhoneCountryCodes()
    {
        var options = new DbContextOptionsBuilder<MemberDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new MemberDbContext(options);

        SeedPhoneCountryCodes(context);
        context.SaveChanges();

        return context;
    }

    private static void SeedPhoneCountryCodes(MemberDbContext context)
    {
        var codes = new[]
        {
            (Id: 1, DialCode: "886", CountryName: "Taiwan", CountryIso: "TW"),
            (Id: 2, DialCode: "1",   CountryName: "United States", CountryIso: "US"),
            (Id: 3, DialCode: "81",  CountryName: "Japan", CountryIso: "JP"),
            (Id: 4, DialCode: "44",  CountryName: "United Kingdom", CountryIso: "GB"),
            (Id: 5, DialCode: "61",  CountryName: "Australia", CountryIso: "AU"),
            (Id: 6, DialCode: "49",  CountryName: "Germany", CountryIso: "DE"),
        };

        foreach (var (id, dialCode, countryName, countryIso) in codes)
        {
            var pcc = CreatePhoneCountryCode(id, dialCode, countryName, countryIso);
            context.PhoneCountryCodes.Add(pcc);
        }
    }

    private static PhoneCountryCode CreatePhoneCountryCode(int id, string dialCode, string countryName, string countryIso)
    {
        var pcc = (PhoneCountryCode)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(PhoneCountryCode));
        typeof(PhoneCountryCode).GetProperty("Id")!.SetValue(pcc, id);
        typeof(PhoneCountryCode).GetProperty("DialCode")!.SetValue(pcc, dialCode);
        typeof(PhoneCountryCode).GetProperty("CountryName")!.SetValue(pcc, countryName);
        typeof(PhoneCountryCode).GetProperty("CountryIso")!.SetValue(pcc, countryIso);
        return pcc;
    }
}
