namespace TradeSettlement;

public sealed record DutyObligation(
    DeclarationLine Line,
    Money CustomsValueLocal,
    Money AdValoremDuty,
    Money SpecificDuty,
    Money TotalDuty);

public sealed class DutyCalculator
{
    private readonly IFxRateProvider _fx;

    public DutyCalculator(IFxRateProvider fx) => _fx = fx;

    public IReadOnlyList<DutyObligation> Compute(Declaration declaration)
    {
        var obligations = new List<DutyObligation>(declaration.Lines.Count);
        foreach (var line in declaration.Lines)
        {
            var customsValueLocal = ConvertToLocal(line.DeclaredValue, declaration.LocalCurrency, declaration.LodgementDate);

            var adValorem = line.DutyRate.Type is DutyType.AdValorem or DutyType.Compound
                ? customsValueLocal.Multiply(line.DutyRate.AdValoremPercent / 100m)
                : Money.Zero(declaration.LocalCurrency);

            var specific = Money.Zero(declaration.LocalCurrency);
            if (line.DutyRate.Type is DutyType.Specific or DutyType.Compound)
            {
                var specificCurrency = line.DutyRate.SpecificCurrency ?? declaration.LocalCurrency;
                var specificForeign = new Money(line.DutyRate.SpecificPerUnit * line.Quantity, specificCurrency);
                specific = ConvertToLocal(specificForeign, declaration.LocalCurrency, declaration.LodgementDate);
            }

            obligations.Add(new DutyObligation(
                Line: line,
                CustomsValueLocal: customsValueLocal,
                AdValoremDuty: adValorem,
                SpecificDuty: specific,
                TotalDuty: adValorem.Add(specific)));
        }
        return obligations;
    }

    private Money ConvertToLocal(Money amount, string localCurrency, DateOnly asOf)
    {
        if (string.Equals(amount.Currency, localCurrency, StringComparison.OrdinalIgnoreCase))
            return amount;
        var quote = _fx.GetRate(amount.Currency, localCurrency, asOf);
        return quote.Convert(amount);
    }
}
