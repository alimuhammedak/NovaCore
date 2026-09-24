using Riok.Mapperly.Abstractions;
using NovaCore.Modules.Payments.Domain;

namespace NovaCore.Modules.Payments.Mapping;

[Mapper]
public static partial class PaymentMapper
{
    // Enum PaymentStatus -> string maps by name automatically.
    public static partial PaymentDto ToDto(Payment payment);

    public static partial IQueryable<PaymentDto> ProjectToDto(this IQueryable<Payment> source);
}
