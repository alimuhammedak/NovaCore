using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace NovaCore.AspNetCore.OpenApi;

/// <summary>
/// Declares the JWT bearer security scheme in the generated OpenAPI document so API explorers
/// (Scalar/Swagger UI) render an "Authorize" affordance. Applied via <c>AddOpenApi</c>.
/// </summary>
public sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    /// <summary>The key of the scheme under <c>components.securitySchemes</c>.</summary>
    public const string SchemeId = "Bearer";

    /// <inheritdoc />
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var scheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Name = "Authorization",
            Description = "Enter a JWT access token (the 'Bearer ' prefix is added automatically)."
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[SchemeId] = scheme;

        return Task.CompletedTask;
    }
}
