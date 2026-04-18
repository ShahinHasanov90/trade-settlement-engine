namespace TradeSettlement;

public enum DutyType { AdValorem, Specific, Compound }

public sealed record DeclarationLine(
    string HsCode,
    string Description,
    Money DeclaredValue,
    decimal Quantity,
    string UnitOfMeasure,
    string OriginCountry,
    DutyRate DutyRate);

public sealed record DutyRate(
    DutyType Type,
    decimal AdValoremPercent = 0m,
    decimal SpecificPerUnit = 0m,
    string? SpecificCurrency = null);

public sealed record Declaration(
    string DeclarationId,
    string ImporterId,
    DateOnly LodgementDate,
    string LocalCurrency,
    IReadOnlyList<DeclarationLine> Lines);
