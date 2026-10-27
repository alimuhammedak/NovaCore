using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NovaCore.Abstractions.MultiTenancy;
using NovaCore.AspNetCore.Authorization;
using NovaCore.AspNetCore.Identity;

namespace NovaCore.MultiTenancy;

/// <summary>
/// The permissions a caller's membership grants in the scope's tenant: those of every role it holds there
/// (<c>MultiTenancy:Roles</c>). Nothing without a tenant: the core sets the scope's tenant, only for a tenant the caller
/// may work in, before it asks the resolvers. The core adds them to what the identity provider's resolver grants.
/// </summary>
internal sealed class MembershipPermissionResolver(
    MultiTenancyDbContext store,
    TenantContext tenantContext,
    IOptions<MultiTenancyOptions> options) : IPermissionResolver
{
    public async Task<IReadOnlyCollection<string>> GetPermissionsAsync(ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var subject = user.FindFirstValue(NovaClaims.Subject);
        if (tenantContext.TenantId is not { } tenantId || string.IsNullOrEmpty(subject))
            return [];

        var roles = await store.Memberships
            .Where(membership => membership.TenantId == tenantId && membership.Subject == subject)
            .Select(membership => membership.Roles)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return roles is null
            ? []
            : roles.SelectMany(role => options.Value.Roles.TryGetValue(role, out var granted) ? granted : [])
                .Distinct(StringComparer.Ordinal)
                .ToArray();
    }
}
