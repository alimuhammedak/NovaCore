using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace NovaCore.Api.Tests;

/// <summary>
/// For the SQLite test databases: stores every <see cref="DateTimeOffset"/> as UTC ticks. SQLite keeps them as text and
/// EF Core can't compare or order them there (docs/plan/RESEARCH.md R-068, R-112), so production code that compares
/// them in SQL, as it should on SQL Server, would fail on SQLite. The tests adapt to the code, not the other way round.
/// The values are UTC (<see cref="TimeProvider.GetUtcNow"/>), so dropping the offset loses nothing.
/// </summary>
/// <remarks>
/// A context gets it with <c>ReplaceService&lt;IModelCustomizer, UtcTicksModelCustomizer&gt;()</c>, or through the EF
/// service provider it uses (<see cref="NovaCoreApiFactory"/>). Raw SQL writes and reads these columns as ticks.
/// </remarks>
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
