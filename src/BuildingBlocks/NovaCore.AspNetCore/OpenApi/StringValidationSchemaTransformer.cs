using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace NovaCore.AspNetCore.OpenApi;

/// <summary>
/// Puts the string rules of two validation attributes into a property's schema, which the document generator leaves
/// out (DAST S-16, ST202): <see cref="RequiredAttribute"/> without <see cref="RequiredAttribute.AllowEmptyStrings"/>
/// refuses an empty or whitespace-only string (<c>minLength: 1</c> unless a larger one is set, and a pattern that asks
/// for one character that isn't whitespace), and <see cref="EmailAddressAttribute"/> refuses a string that isn't an
/// address (<c>format: email</c>). The attributes are read where the validation reads them, on the property
/// (D-103 (1)); a schema that already has a pattern or a format keeps it.
/// </summary>
internal sealed class StringValidationSchemaTransformer : IOpenApiSchemaTransformer
{
    // string.IsNullOrWhiteSpace is char.IsWhiteSpace; the regex \s of JSON Schema (ECMA-262) covers it except U+0085.
    internal const string NotWhiteSpacePattern = @"[^\s\u0085]";

    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        if (context.JsonPropertyInfo?.AttributeProvider is not { } attributes
            || schema.Type is not { } type || !type.HasFlag(JsonSchemaType.String))
            return Task.CompletedTask;

        if (attributes.GetCustomAttributes(typeof(RequiredAttribute), inherit: true) is [RequiredAttribute { AllowEmptyStrings: false }, ..])
        {
            if (schema.MinLength is null or < 1)
                schema.MinLength = 1;
            schema.Pattern ??= NotWhiteSpacePattern;
        }

        if (attributes.IsDefined(typeof(EmailAddressAttribute), inherit: true))
            schema.Format ??= "email";

        return Task.CompletedTask;
    }
}
