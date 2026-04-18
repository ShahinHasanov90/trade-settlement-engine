using System.Collections.Concurrent;
using TradeSettlement;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IFxRateProvider>(sp =>
{
    var provider = new InMemoryFxRateProvider();
    provider.AddQuote(new FxQuote("USD", "AZN", 1.70m, DateOnly.FromDateTime(DateTime.UtcNow), "seed"));
    provider.AddQuote(new FxQuote("EUR", "AZN", 1.85m, DateOnly.FromDateTime(DateTime.UtcNow), "seed"));
    return provider;
});
builder.Services.AddSingleton<DutyCalculator>();
builder.Services.AddSingleton<SettlementEngine>();

var app = builder.Build();
var store = new ConcurrentDictionary<string, Settlement>();

app.MapPost("/settlements", (Declaration declaration, SettlementEngine engine) =>
{
    var settlement = engine.Assess(declaration, actor: "api");
    store[declaration.DeclarationId] = settlement;
    return Results.Created($"/settlements/{declaration.DeclarationId}", new
    {
        declaration.DeclarationId,
        settlement.Assessed,
        settlement.Status
    });
});

app.MapGet("/settlements/{id}", (string id) =>
    store.TryGetValue(id, out var s)
        ? Results.Ok(new { s.DeclarationId, s.Assessed, s.Paid, s.Refunded, s.Balance, s.Status, Events = s.Events })
        : Results.NotFound());

app.MapPost("/settlements/{id}/payments", (string id, PaymentRequest req, SettlementEngine engine) =>
{
    if (!store.TryGetValue(id, out var settlement)) return Results.NotFound();
    engine.Pay(settlement, new Money(req.Amount, req.Currency), req.Actor, req.Reason);
    return Results.Ok(new { settlement.Balance, settlement.Status });
});

app.MapPost("/settlements/{id}/refunds", (string id, RefundRequest req, SettlementEngine engine) =>
{
    if (!store.TryGetValue(id, out var settlement)) return Results.NotFound();
    engine.Refund(settlement, new Money(req.Amount, req.Currency), req.Actor, req.Reason);
    return Results.Ok(new { settlement.Balance, settlement.Status });
});

app.Run();

public record PaymentRequest(decimal Amount, string Currency, string Actor, string Reason);
public record RefundRequest(decimal Amount, string Currency, string Actor, string Reason);
