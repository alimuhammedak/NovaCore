using System.ComponentModel.DataAnnotations;

namespace NovaCore.Modules.Catalog.Infrastructure;

/// <summary>
/// The value fits a <c>decimal(precision, scale)</c> column as it is. SQL Server rounds the decimal places a column doesn't
/// keep (a price of 0.001 is stored as 0.00) and refuses more digits before the point, so without this rule the API would
/// answer 201 for a value it stores differently, or 500 (docs/plan/RESEARCH.md R-111).
/// </summary>
/// <remarks>A rule on a single value is a custom attribute (root CLAUDE.md, R-052).</remarks>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
internal sealed class DecimalColumnAttribute(int precision, int scale) : ValidationAttribute(
    $"The {{0}} field must have at most {scale} decimal places and {precision - scale} digits before the decimal point.")
{
    // 10^(precision - scale): the smallest value with one integer digit too many.
    private readonly decimal _limit = Enumerable.Repeat(10m, precision - scale).Aggregate(1m, (product, ten) => product * ten);

    public override bool IsValid(object? value) =>
        value is not decimal number
        || (decimal.Round(number, scale) == number && Math.Abs(number) < _limit);
}
