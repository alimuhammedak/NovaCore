using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.HttpResults;

namespace NovaCore.Identity.OpenIddict;

/// <summary>
/// A URL on this host only (a path such as <c>/connect/authorize?...</c>), never another site's: an open redirect would
/// hand the signed-in browser to it. The framework's own check, the one <c>LocalRedirect</c> applies.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
internal sealed class LocalUrlAttribute : ValidationAttribute
{
    public LocalUrlAttribute()
        : base("The return URL must be a path on this host.")
    {
    }

    public override bool IsValid(object? value) =>
        value is null || (value is string url && (url.Length == 0 || RedirectHttpResult.IsLocalUrl(url)));
}
