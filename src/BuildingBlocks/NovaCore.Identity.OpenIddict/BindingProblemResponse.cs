using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace NovaCore.Identity.OpenIddict;

/// <summary>
/// Documents the framework's 400 beside an operation's own: a query or route value the framework can't bind (a missing
/// required value, a <c>Guid</c> that isn't one) answers 400 with ProblemDetails, while the handler answers its own 400
/// with another body. .NET 10's document keeps one declaration per status code, so a second
/// <c>ProducesProblem(400)</c> would replace the handler's; this operation transformer adds the second body instead
/// (DAST S-15, ST105; .NET 11 keeps every declaration).
/// </summary>
internal static class BindingProblemResponse
{
    private const string ProblemSchemaId = nameof(ProblemDetails);

    public static async Task AddAsync(
        OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        if (operation.Responses?.TryGetValue("400", out var badRequest) is not true || badRequest.Content is not { } content)
            return;

        if (context.Document is { } document && !(document.Components?.Schemas?.ContainsKey(ProblemSchemaId) ?? false))
            document.AddComponent(
                ProblemSchemaId, await context.GetOrCreateSchemaAsync(typeof(ProblemDetails), null, cancellationToken));

        content["application/problem+json"] = new OpenApiMediaType
        {
            Schema = new OpenApiSchemaReference(ProblemSchemaId, context.Document)
        };
    }
}
