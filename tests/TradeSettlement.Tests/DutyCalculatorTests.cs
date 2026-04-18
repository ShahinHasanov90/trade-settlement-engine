using TradeSettlement;
using Xunit;

namespace TradeSettlement.Tests;

public class DutyCalculatorTests
{
    private static DutyCalculator NewCalculator(string local = "AZN")
    {
        var fx = new InMemoryFxRateProvider();
        fx.AddQuote(new FxQuote("USD", local, 1.70m, new DateOnly(2022, 11, 10), "seed"));
        fx.AddQuote(new FxQuote("EUR", local, 1.85m, new DateOnly(2022, 11, 10), "seed"));
        return new DutyCalculator(fx);
    }

    [Fact]
    public void AdValorem_uses_customs_value_in_local_currency()
    {
        var calc = NewCalculator();
        var line = new DeclarationLine(
            HsCode: "8471.30",
            Description: "Portable ADP machines",
            DeclaredValue: new Money(1000m, "USD"),
            Quantity: 10,
            UnitOfMeasure: "pcs",
            OriginCountry: "CN",
            DutyRate: new DutyRate(DutyType.AdValorem, AdValoremPercent: 5m));
        var decl = new Declaration("D1", "IMP-1", new DateOnly(2022, 11, 10), "AZN", new[] { line });

        var obligations = calc.Compute(decl);

        Assert.Single(obligations);
        Assert.Equal(new Money(1700m, "AZN"), obligations[0].CustomsValueLocal);
        Assert.Equal(new Money(85m, "AZN"), obligations[0].AdValoremDuty);
        Assert.Equal(new Money(85m, "AZN"), obligations[0].TotalDuty);
    }

    [Fact]
    public void Compound_duty_sums_ad_valorem_and_specific()
    {
        var calc = NewCalculator();
        var line = new DeclarationLine(
            HsCode: "2208.60",
            Description: "Vodka",
            DeclaredValue: new Money(500m, "EUR"),
            Quantity: 100,
            UnitOfMeasure: "L",
            OriginCountry: "PL",
            DutyRate: new DutyRate(DutyType.Compound, AdValoremPercent: 10m, SpecificPerUnit: 2m, SpecificCurrency: "EUR"));
        var decl = new Declaration("D2", "IMP-2", new DateOnly(2022, 11, 10), "AZN", new[] { line });

        var obligations = calc.Compute(decl);

        Assert.Equal(new Money(925m, "AZN"), obligations[0].CustomsValueLocal);
        Assert.Equal(new Money(92.5m, "AZN"), obligations[0].AdValoremDuty);
        Assert.Equal(new Money(370m, "AZN"), obligations[0].SpecificDuty);
        Assert.Equal(new Money(462.5m, "AZN"), obligations[0].TotalDuty);
    }
}
