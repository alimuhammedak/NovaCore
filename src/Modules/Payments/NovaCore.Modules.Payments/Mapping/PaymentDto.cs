namespace NovaCore.Modules.Payments.Mapping;

public sealed record PaymentDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal Amount,
    string Status,
    DateTimeOffset CreatedAtUtc);
