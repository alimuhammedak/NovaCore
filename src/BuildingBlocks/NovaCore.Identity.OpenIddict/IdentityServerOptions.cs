using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace NovaCore.Identity.OpenIddict;

/// <summary>Bound from the <c>Identity</c> configuration section and validated when the host starts.</summary>
internal sealed class IdentityServerOptions
{
    public const string SectionName = "Identity";

    /// <summary>
    /// The client applications, synced into the store when <see cref="IdentitySeedOptions.SeedOnStartup"/> is true. A
    /// client with redirect URIs signs users in (Authorization Code with PKCE, refresh tokens): public without a secret,
    /// confidential with one. A client without them is a service: it authenticates with its secret (client credentials)
    /// and holds the <see cref="IdentityClientOptions.Permissions"/> configured for it.
    /// </summary>
    [ValidateEnumeratedItems]
    public List<IdentityClientOptions> Clients { get; set; } = [];

    public IdentitySeedOptions Seed { get; set; } = new();
}

/// <summary>A client application of the provider (<c>Identity:Clients:n</c>).</summary>
internal sealed class IdentityClientOptions
{
    /// <summary>The shortest <see cref="ClientSecret"/>: 32 characters hold 128 bits or more when generated (RFC 6749 §10.10).</summary>
    public const int MinimumSecretLength = 32;

    /// <summary>The OAuth <c>client_id</c>. Not a Guid: a Guid <c>sub</c> is a user (the permission resolver relies on it).</summary>
    [Required]
    public string ClientId { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    /// <summary>
    /// The secret of a confidential client, at least <see cref="MinimumSecretLength"/> characters, from user secrets or the
    /// environment, never from appsettings.
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>Where the authorization code is sent. Set for a client that signs users in.</summary>
    public List<Uri> RedirectUris { get; set; } = [];

    /// <summary>Where the provider may send the browser after sign-out.</summary>
    public List<Uri> PostLogoutRedirectUris { get; set; } = [];

    /// <summary>
    /// A service client's permissions, e.g. <c>catalog:products:read</c>. The permission resolver grants them to its
    /// tokens, whose <c>sub</c> is the client id. A client that signs users in gets its users' permissions instead.
    /// </summary>
    public List<string> Permissions { get; set; } = [];

    public bool SignsInUsers => RedirectUris.Count > 0;
}

/// <summary>Startup seeding (<c>Identity:Seed</c>).</summary>
internal sealed class IdentitySeedOptions
{
    /// <summary>
    /// Seed on startup: the Administrator and User roles with their permissions, the initial administrator and the
    /// clients. Needs a migrated database.
    /// </summary>
    public bool SeedOnStartup { get; set; }

    /// <summary>The initial administrator's email. Skipped while either this or the password is unset.</summary>
    public string? AdminEmail { get; set; }

    /// <summary>The initial administrator's password, from user secrets or the environment, never from appsettings.</summary>
    public string? AdminPassword { get; set; }

    /// <summary>
    /// The permissions of the User role, which every registered account gets, e.g. <c>catalog:products:read</c>: each
    /// one declared by a module, none of the provider's own (checked on start). Seeding adds the missing ones and never
    /// removes one.
    /// </summary>
    public List<string> UserPermissions { get; set; } = [];
}

/// <summary>Source-generated validator for the data-annotation rules (docs/plan/RESEARCH.md R-048).</summary>
[OptionsValidator]
internal sealed partial class IdentityServerOptionsValidator : IValidateOptions<IdentityServerOptions>;
