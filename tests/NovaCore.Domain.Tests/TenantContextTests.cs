using NovaCore.Abstractions.MultiTenancy;
using Xunit;

namespace NovaCore.Domain.Tests;

/// <summary>A scope works in one tenant, given once (docs/plan/DECISIONS.md D-076 (5)).</summary>
public class TenantContextTests
{
    [Fact]
    public void A_new_scope_has_no_tenant()
    {
        Assert.Null(new TenantContext().TenantId);
    }

    [Fact]
    public void A_scope_keeps_the_tenant_it_was_given_and_takes_it_again()
    {
        var tenant = Guid.NewGuid();
        var context = new TenantContext();

        context.Use(tenant);
        context.Use(tenant);

        Assert.Equal(tenant, context.TenantId);
    }

    [Fact]
    public void A_scope_does_not_move_to_another_tenant()
    {
        var tenant = Guid.NewGuid();
        var context = new TenantContext();
        context.Use(tenant);

        Assert.Throws<InvalidOperationException>(() => context.Use(Guid.NewGuid()));
        Assert.Equal(tenant, context.TenantId);
    }

    [Fact]
    public void The_empty_id_is_no_tenant()
    {
        Assert.Throws<ArgumentException>(() => new TenantContext().Use(Guid.Empty));
    }
}
