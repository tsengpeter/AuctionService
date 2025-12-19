namespace BiddingService.Core.ValueObjects;

public class BidAmount
{
    public decimal Value { get; private set; }

    private BidAmount() { } // EF Core constructor

    public BidAmount(decimal value)
    {
        if (value <= 0)
            throw new ArgumentException("Bid amount must be greater than zero", nameof(value));

        Value = value;
    }

    public static implicit operator decimal(BidAmount amount) => amount.Value;
    public static explicit operator BidAmount(decimal value) => new BidAmount(value);

    public override string ToString() => Value.ToString("F2");
}