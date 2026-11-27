using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace NovaCore.AspNetCore.OpenApi;

/// <summary>
/// Documents an integer route, query or header parameter as the integer its binder accepts. The generator describes every
/// integer with the JSON options, whose <c>AllowReadingFromString</c> (the web default) makes it
/// <c>integer|string</c> with an unbounded digits pattern; a parameter is text the binder parses with the type's
/// <c>TryParse</c>, not JSON, so a value past the type's range matched the schema and got a 400 (DAST S-16, ST202). The
/// schema becomes <c>integer</c> with the type's range; a range the schema already has stays. The JSON options stay as
/// they are: <c>Strict</c>, Microsoft's way to drop the string form, would also refuse quoted numbers in request bodies.
/// </summary>
internal sealed class IntegerParameterSchemaTransformer : IOpenApiSchemaTransformer
{
    // A body's (and a form's) numbers are JSON or form values the JSON options do read from strings.
    private static readonly BindingSource[] TextSources = [BindingSource.Path, BindingSource.Query, BindingSource.Header];

    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        if (context.ParameterDescription?.Source is not { } source || !TextSources.Contains(source) || context.JsonPropertyInfo is not null
            || schema.Type is not { } type || !type.HasFlag(JsonSchemaType.Integer) || !type.HasFlag(JsonSchemaType.String)
            || Range(Nullable.GetUnderlyingType(context.JsonTypeInfo.Type) ?? context.JsonTypeInfo.Type) is not var (minimum, maximum))
            return Task.CompletedTask;

        schema.Type = type & ~JsonSchemaType.String;
        schema.Pattern = null;
        schema.Minimum ??= minimum;
        schema.Maximum ??= maximum;
        return Task.CompletedTask;
    }

    private static (string Minimum, string Maximum)? Range(Type type) =>
        type == typeof(int) ? Of(int.MinValue, int.MaxValue)
        : type == typeof(long) ? Of(long.MinValue, long.MaxValue)
        : type == typeof(short) ? Of(short.MinValue, short.MaxValue)
        : type == typeof(sbyte) ? Of(sbyte.MinValue, sbyte.MaxValue)
        : type == typeof(byte) ? Of(byte.MinValue, byte.MaxValue)
        : type == typeof(ushort) ? Of(ushort.MinValue, ushort.MaxValue)
        : type == typeof(uint) ? Of(uint.MinValue, uint.MaxValue)
        : type == typeof(ulong) ? Of(ulong.MinValue, ulong.MaxValue)
        : null;

    private static (string, string) Of<T>(T minimum, T maximum) where T : IFormattable =>
        (minimum.ToString(null, CultureInfo.InvariantCulture), maximum.ToString(null, CultureInfo.InvariantCulture));
}
