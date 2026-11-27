using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace NovaCore.AspNetCore.OpenApi;

/// <summary>
/// Documents the authorization gate's answers on every operation it guards: 401 on an operation that isn't anonymous
/// (authorization is closed by default, <c>AddCoreAuthorization</c>'s fallback policy), 403 on one that names a
/// requirement beyond a signed-in caller (a permission, a tenant, a policy or roles). Both bodies are ProblemDetails
/// (<c>UseStatusCodePages</c> with <c>AddProblemDetails</c>). A response the operation already declares is kept.
/// </summary>
internal sealed class AuthorizationResponsesTransformer : IOpenApiOperationTransformer
{
    private const string ProblemSchemaId = nameof(ProblemDetails);

    public async Task TransformAsync(
        OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;
        if (metadata.OfType<IAllowAnonymous>().Any())
            return;

        if (context.Document is { } document && !(document.Components?.Schemas?.ContainsKey(ProblemSchemaId) ?? false))
            document.AddComponent(
                ProblemSchemaId, await context.GetOrCreateSchemaAsync(typeof(ProblemDetails), null, cancellationToken));

        operation.Responses ??= [];
        Add(operation.Responses, StatusCodes.Status401Unauthorized, "Unauthorized", context.Document);
        if (NamesRequirement(metadata))
            Add(operation.Responses, StatusCodes.Status403Forbidden, "Forbidden", context.Document);
    }

    private static bool NamesRequirement(IList<object> metadata) =>
        metadata.OfType<IAuthorizationRequirementData>().Any()
        || metadata.OfType<IAuthorizeData>().Any(data => !string.IsNullOrEmpty(data.Policy) || !string.IsNullOrEmpty(data.Roles))
        || metadata.OfType<AuthorizationPolicy>()
            .Any(policy => policy.Requirements.Any(requirement => requirement is not DenyAnonymousAuthorizationRequirement));

    private static void Add(OpenApiResponses responses, int status, string description, OpenApiDocument? document)
    {
        var key = status.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (responses.ContainsKey(key))
            return;

        responses[key] = new OpenApiResponse
        {
            Description = description,
            Content = new Dictionary<string, OpenApiMediaType>(StringComparer.Ordinal)
            {
                ["application/problem+json"] = new() { Schema = new OpenApiSchemaReference(ProblemSchemaId, document) }
            }
        };
    }
}
