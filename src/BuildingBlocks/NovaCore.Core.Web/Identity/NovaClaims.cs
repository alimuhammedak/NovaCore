namespace NovaCore.Core.Web.Identity;

/// <summary>
/// The raw JWT claim names NovaCore issues and validates against. We disable inbound claim mapping
/// (<c>MapInboundClaims = false</c>) and use these short names consistently across token creation,
/// validation, and <see cref="HttpContextCurrentUser"/> — so there is exactly one spelling of each.
/// </summary>
public static class NovaClaims
{
    public const string Subject = "sub";
    public const string Name = "name";
    public const string Email = "email";
    public const string Role = "role";

    /// <summary>Fine-grained permission claim, e.g. <c>catalog:products:create</c>. Emitted once per permission.</summary>
    public const string Permission = "permission";
}
