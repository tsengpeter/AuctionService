namespace Member.Domain;

public class PhoneCountryCode
{
    public int Id { get; private set; }
    public string DialCode { get; private set; } = string.Empty;
    public string CountryName { get; private set; } = string.Empty;
    public string CountryIso { get; private set; } = string.Empty;

    private PhoneCountryCode() { }
}
