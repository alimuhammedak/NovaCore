using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace NovaCore.Identity.OpenIddict;

/// <summary>Bound from the <c>Identity</c> configuration section and validated when the host starts.</summary>
internal sealed class IdentityServerOptions
{
    public const string SectionName = "Identity";

    /// <summary>
    /// The client applications, synced into the store when <see cref="IdentitySeedOptions.SeedOnStartup"/> is true. Each
    /// signs users in through its redirect URIs (Authorization Code with PKCE, refresh tokens): public without a secret,
    /// confidential with one.
    /// </summary>
    [ValidateEnumeratedItems]
    public List<IdentityClientOptions> Clients { get; set; } = [];

    public IdentitySeedOptions Seed { get; set; } = new();
}

/// <summary>A client application of the provider (<c>Identity:Clients:n</c>).</summary>
internal sealed class IdentityClientOptions
{
    /// <summary>The OAuth <c>client_id</c>. Not a Guid: a Guid <c>sub</c> is a user (the permission resolver relies on it).</summary>
    [Required]
    public string ClientId { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    /// <summary>The secret of a confidential client, from user secrets or the environment, never from appsettings.</summary>
    public string? ClientSecret { get; set; }

    /// <summary>Where the authorization code is sent. Set for a client that signs users in.</summary>
    public List<Uri> RedirectUris { get; set; } = [];

    /// <summary>Where the provider may send the browser after sign-out.</summary>
    public List<Uri> PostLogoutRedirectUris { get; set; } = [];
}

/// <summary>Startup seeding (<c>Identity:Seed</c>).</summary>
internal sealed class IdentitySeedOptions
{
    /// <summary>Sync the clients into the store on startup. Needs a migrated database.</summary>
    public bool SeedOnStartup { get; set; }
}

/// <summary>Source-generated validator for the data-annotation rules (docs/plan/RESEARCH.md R-048).</summary>
[OptionsValidator]
internal sealed partial class IdentityServerOptionsValidator : IValidateOptions<IdentityServerOptions>;
