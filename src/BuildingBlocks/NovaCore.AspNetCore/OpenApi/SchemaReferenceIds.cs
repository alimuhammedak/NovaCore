using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.OpenApi;

namespace NovaCore.AspNetCore.OpenApi;

/// <summary>
/// Names the schema of a type nested in a public type after the declaring types too (<c>CreatePayment.Request</c> is
/// <c>CreatePaymentRequest</c>). The default names a schema after the type alone, so the vertical slices' nested
/// <c>Request</c> and <c>Response</c> records (a feature is a public static class) of different features shared one
/// schema, and an operation was documented with another feature's body (S-20). A type nested in an internal class (the
/// identity provider's endpoint classes, whose records carry names of their own) and every other type keep the default
/// name, so their schemas don't change.
/// </summary>
internal static class SchemaReferenceIds
{
    public static string? Create(JsonTypeInfo jsonTypeInfo)
    {
        var id = OpenApiOptions.CreateDefaultSchemaReferenceId(jsonTypeInfo);
        if (id is null)
            return null;

        for (var type = jsonTypeInfo.Type; type.IsNestedPublic && type.DeclaringType is { IsVisible: true } outer; type = outer)
            id = outer.Name + id;

        return id;
    }
}
