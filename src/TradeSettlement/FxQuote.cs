namespace TradeSettlement;

public readonly record struct FxQuote(string Base, string Quote, decimal Rate, DateOnly EffectiveDate, string Source)
{
    public Money Convert(Money amount)
    {
        if (!string.Equals(amount.Currency, Base, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"FX quote base {Base} does not match amount currency {amount.Currency}.");
        return new Money(amount.Amount * Rate, Quote);
    }
}

public interface IFxRateProvider
{
    FxQuote GetRate(string baseCurrency, string quoteCurrency, DateOnly asOf);
}

public sealed class InMemoryFxRateProvider : IFxRateProvider
{
    private readonly Dictionary<(string Base, string Quote, DateOnly Date), FxQuote> _quotes = new();

    public void AddQuote(FxQuote quote) =>
        _quotes[(quote.Base.ToUpperInvariant(), quote.Quote.ToUpperInvariant(), quote.EffectiveDate)] = quote;

    public FxQuote GetRate(string baseCurrency, string quoteCurrency, DateOnly asOf)
    {
        var key = (baseCurrency.ToUpperInvariant(), quoteCurrency.ToUpperInvariant(), asOf);
        if (_quotes.TryGetValue(key, out var quote))
            return quote;

        if (string.Equals(baseCurrency, quoteCurrency, StringComparison.OrdinalIgnoreCase))
            return new FxQuote(baseCurrency, quoteCurrency, 1m, asOf, "identity");

        throw new KeyNotFoundException($"No FX quote for {baseCurrency}/{quoteCurrency} on {asOf:yyyy-MM-dd}.");
    }
}
