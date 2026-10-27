using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NovaCore.AspNetCore.Authorization;
using NovaCore.AspNetCore.Identity;

namespace NovaCore.MultiTenancy;

/// <summary>
/// The store's answer to the core: the tenant a request names (where <c>AddCoreMultiTenancy</c> said to read it), and
/// whether the caller is a member of it while it is active. One query; the core caches the answer.
/// </summary>
internal sealed class TenantResolver(MultiTenancyDbContext store, IOptions<MultiTenancyOptions> options) : ITenantResolver
{
    public Guid? RequestedTenant(HttpContext context) =>
        Guid.TryParse(options.Value.TenantOf(context), out var tenantId) && tenantId != Guid.Empty ? tenantId : null;

    public async Task<bool> CanAccessAsync(ClaimsPrincipal user, Guid tenantId, CancellationToken cancellationToken)
    {
        var subject = user.FindFirstValue(NovaClaims.Subject);
        if (string.IsNullOrEmpty(subject))
            return false;

        return await store.Memberships.AnyAsync(
            membership => membership.TenantId == tenantId
                          && membership.Subject == subject
                          && store.Tenants.Any(tenant => tenant.Id == tenantId && tenant.Status == TenantStatus.Active),
            cancellationToken).ConfigureAwait(false);
    }
}
