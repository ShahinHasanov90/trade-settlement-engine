namespace TradeSettlement;

public enum SettlementEventType { Assessment, Payment, Refund, Adjustment, Reversal }

public enum SettlementStatus { Open, PartiallyPaid, Paid, Overpaid, Refunded, Reversed }

public sealed record SettlementEvent(
    Guid EventId,
    string DeclarationId,
    SettlementEventType Type,
    Money Amount,
    string Actor,
    string Reason,
    DateTime OccurredAt);

public sealed class Settlement
{
    private readonly List<SettlementEvent> _events = new();

    public string DeclarationId { get; }
    public string LocalCurrency { get; }
    public Money Assessed { get; private set; }
    public Money Paid { get; private set; }
    public Money Refunded { get; private set; }

    public IReadOnlyList<SettlementEvent> Events => _events;

    public Money Balance => Assessed.Subtract(Paid).Add(Refunded);

    public SettlementStatus Status
    {
        get
        {
            if (_events.Any(e => e.Type == SettlementEventType.Reversal)) return SettlementStatus.Reversed;
            if (Refunded.Amount >= Assessed.Amount && Assessed.Amount > 0m) return SettlementStatus.Refunded;
            if (Paid.Amount == 0m) return SettlementStatus.Open;
            if (Paid.Amount < Assessed.Amount) return SettlementStatus.PartiallyPaid;
            if (Paid.Amount == Assessed.Amount) return SettlementStatus.Paid;
            return SettlementStatus.Overpaid;
        }
    }

    public Settlement(string declarationId, string localCurrency)
    {
        DeclarationId = declarationId;
        LocalCurrency = localCurrency;
        Assessed = Money.Zero(localCurrency);
        Paid = Money.Zero(localCurrency);
        Refunded = Money.Zero(localCurrency);
    }

    public void Apply(SettlementEvent evt)
    {
        if (!string.Equals(evt.Amount.Currency, LocalCurrency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Event currency {evt.Amount.Currency} must match settlement {LocalCurrency}.");

        switch (evt.Type)
        {
            case SettlementEventType.Assessment:
                Assessed = Assessed.Add(evt.Amount);
                break;
            case SettlementEventType.Payment:
                Paid = Paid.Add(evt.Amount);
                break;
            case SettlementEventType.Refund:
                Refunded = Refunded.Add(evt.Amount);
                break;
            case SettlementEventType.Adjustment:
                Assessed = Assessed.Add(evt.Amount);
                break;
            case SettlementEventType.Reversal:
                break;
        }
        _events.Add(evt);
    }
}

public sealed class SettlementEngine
{
    private readonly DutyCalculator _calculator;

    public SettlementEngine(DutyCalculator calculator) => _calculator = calculator;

    public Settlement Assess(Declaration declaration, string actor)
    {
        var settlement = new Settlement(declaration.DeclarationId, declaration.LocalCurrency);
        var obligations = _calculator.Compute(declaration);
        var total = obligations.Aggregate(
            Money.Zero(declaration.LocalCurrency),
            (acc, o) => acc.Add(o.TotalDuty));

        settlement.Apply(new SettlementEvent(
            EventId: Guid.NewGuid(),
            DeclarationId: declaration.DeclarationId,
            Type: SettlementEventType.Assessment,
            Amount: total,
            Actor: actor,
            Reason: $"Initial assessment — {obligations.Count} line(s)",
            OccurredAt: DateTime.UtcNow));
        return settlement;
    }

    public void Pay(Settlement settlement, Money amount, string actor, string reason)
    {
        settlement.Apply(new SettlementEvent(
            Guid.NewGuid(), settlement.DeclarationId, SettlementEventType.Payment, amount, actor, reason, DateTime.UtcNow));
    }

    public void Refund(Settlement settlement, Money amount, string actor, string reason)
    {
        if (amount.Amount > settlement.Paid.Amount)
            throw new InvalidOperationException("Refund cannot exceed paid amount.");
        settlement.Apply(new SettlementEvent(
            Guid.NewGuid(), settlement.DeclarationId, SettlementEventType.Refund, amount, actor, reason, DateTime.UtcNow));
    }
}
