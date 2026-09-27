using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using NovaCore.AspNetCore.Auth;

namespace NovaCore.Identity.OpenIddict;

/// <summary>
/// Declares this provider's Authorization Code flow in the OpenAPI document, so an API explorer (Scalar) can sign a user
/// in with PKCE: the authorization and token endpoints under <c>Jwt:Authority</c> and the scopes the provider supports.
/// An explorer can only use a security scheme the document declares.
/// </summary>
internal sealed class OAuthSecuritySchemeTransformer(IOptions<JwtOptions> jwt) : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var authority = jwt.Value.Authority.TrimEnd('/');

        var scheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Description = "Sign in with the host's OpenID Connect provider (Authorization Code with PKCE).",
            Flows = new OpenApiOAuthFlows
            {
                AuthorizationCode = new OpenApiOAuthFlow
                {
                    AuthorizationUrl = new Uri(authority + "/connect/authorize"),
                    TokenUrl = new Uri(authority + "/connect/token"),
                    Scopes = new Dictionary<string, string>
                    {
                        ["openid"] = "Sign in",
                        ["profile"] = "Name",
                        ["email"] = "Email address",
                        ["roles"] = "Roles",
                        ["offline_access"] = "Refresh tokens"
                    }
                }
            }
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[IdentityServerHostingExtensions.OpenApiSecuritySchemeId] = scheme;

        return Task.CompletedTask;
    }
}
