using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MyService.Api.Tests;

/// <summary>
/// Stores every <see cref="DateTimeOffset"/> of the SQLite test databases as UTC ticks. SQLite keeps them as text and EF
/// Core can't compare or order them there, while the host compares them in SQL, as it should on SQL Server (the outbox
/// dispatcher deletes processed messages by age). The values are UTC, so dropping the offset loses nothing.
/// </summary>
public sealed class UtcTicksModelCustomizer(ModelCustomizerDependencies dependencies) : RelationalModelCustomizer(dependencies)
{
    private static readonly ValueConverter<DateTimeOffset, long> UtcTicks =
        new(value => value.UtcTicks, ticks => new DateTimeOffset(ticks, TimeSpan.Zero));

    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        base.Customize(modelBuilder, context);

        foreach (var property in modelBuilder.Model.GetEntityTypes()
                     .SelectMany(entity => entity.GetProperties())
                     .Where(property => property.ClrType == typeof(DateTimeOffset) || property.ClrType == typeof(DateTimeOffset?)))
            property.SetValueConverter(UtcTicks);
    }
}
