# trade-settlement-engine

Multi-currency trade settlement engine for customs duty payments, FX reconciliation, and refund processing. Built on .NET 8.

## What it does

Customs duty obligations are denominated in the importer's local currency, but invoices, freight, and insurance are frequently in foreign currencies. This engine reconciles declared values against FX rates at the time of lodgement, computes payable duties, and handles settlement events (payment, refund, overpayment, short-payment) with a full audit trail.

## Core capabilities

- **FX rate provider** — pluggable provider interface with cached daily rates. Ships with an in-memory store; adapters for ECB daily, CBR (Central Bank of Russia), and AZN/CBAR can be wired in.
- **Duty calculation** — ad valorem, specific, and compound duty models. FTA preference handling via certificate-of-origin validation.
- **Settlement engine** — journal-style posting of debit/credit entries against importer accounts. Supports partial payments, overpayments (credit note generation), and short-payments (follow-up obligation).
- **Refund processor** — post-release refund workflow for returned goods, overpaid duty, tariff reclassification.
- **Audit trail** — every settlement produces an immutable event record (`SettlementEvent`) with actor, reason code, reference to source declaration.

## Project layout

```
src/
  TradeSettlement/          class library — core domain + engine
  TradeSettlement.Api/      ASP.NET Core minimal API
tests/
  TradeSettlement.Tests/    xUnit unit tests
```

## Build

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project src/TradeSettlement.Api
```

## API surface (Api project)

```
POST /settlements          submit a settlement request
GET  /settlements/{id}     fetch settlement state
POST /settlements/{id}/refund   issue a refund
GET  /fx/{base}/{quote}    fetch cached rate
```

## Domain model

- `Declaration` — the customs declaration being settled. Contains line items with HS code, declared value in foreign currency, quantity, origin.
- `DutyObligation` — computed duty amounts per line, by duty type (ad valorem / specific / compound).
- `Settlement` — aggregate of all payments, refunds, and adjustments against a declaration.
- `SettlementEvent` — append-only event record (Payment, Refund, Adjustment, Reversal).
- `FxQuote` — FX rate snapshot with effective date and source.

## Why this design

Duty disputes and refund cases can surface months or years after the original settlement. The append-only event log makes reconstruction of the payment history deterministic even after schema evolution, which is what customs auditors and importers both need.

## License

MIT
