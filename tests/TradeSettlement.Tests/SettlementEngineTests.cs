using TradeSettlement;
using Xunit;

namespace TradeSettlement.Tests;

public class SettlementEngineTests
{
    private static SettlementEngine NewEngine()
    {
        var fx = new InMemoryFxRateProvider();
        fx.AddQuote(new FxQuote("USD", "AZN", 1.70m, new DateOnly(2022, 11, 10), "seed"));
        return new SettlementEngine(new DutyCalculator(fx));
    }

    private static Declaration SampleDeclaration() => new(
        DeclarationId: "D1",
        ImporterId: "IMP-1",
        LodgementDate: new DateOnly(2022, 11, 10),
        LocalCurrency: "AZN",
        Lines: new[]
        {
            new DeclarationLine("8471.30", "Laptops", new Money(1000m, "USD"), 10, "pcs", "CN",
                new DutyRate(DutyType.AdValorem, AdValoremPercent: 5m))
        });

    [Fact]
    public void Assess_creates_open_settlement_with_total_duty()
    {
        var engine = NewEngine();
        var s = engine.Assess(SampleDeclaration(), "tester");
        Assert.Equal(SettlementStatus.Open, s.Status);
        Assert.Equal(new Money(85m, "AZN"), s.Assessed);
    }

    [Fact]
    public void Partial_payment_transitions_to_partially_paid()
    {
        var engine = NewEngine();
        var s = engine.Assess(SampleDeclaration(), "tester");
        engine.Pay(s, new Money(40m, "AZN"), "cashier", "first installment");
        Assert.Equal(SettlementStatus.PartiallyPaid, s.Status);
        Assert.Equal(new Money(45m, "AZN"), s.Balance);
    }

    [Fact]
    public void Full_payment_transitions_to_paid()
    {
        var engine = NewEngine();
        var s = engine.Assess(SampleDeclaration(), "tester");
        engine.Pay(s, new Money(85m, "AZN"), "cashier", "settled");
        Assert.Equal(SettlementStatus.Paid, s.Status);
    }

    [Fact]
    public void Refund_exceeding_paid_is_rejected()
    {
        var engine = NewEngine();
        var s = engine.Assess(SampleDeclaration(), "tester");
        engine.Pay(s, new Money(50m, "AZN"), "cashier", "partial");
        Assert.Throws<InvalidOperationException>(() =>
            engine.Refund(s, new Money(80m, "AZN"), "cashier", "oops"));
    }
}
