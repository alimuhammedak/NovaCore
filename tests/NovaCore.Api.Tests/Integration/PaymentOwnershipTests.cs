using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.Abstractions.Identity;
using NovaCore.AspNetCore.Identity;
using NovaCore.Modules.Payments.Authorization;
using NovaCore.Modules.Payments.Domain;
using NovaCore.Modules.Payments.Features;
using NovaCore.Modules.Payments.Infrastructure;
using Xunit;

namespace NovaCore.Api.Tests.Integration;

/// <summary>
/// DECISIONS D-047: a payment belongs to the caller who created it (the token's <c>sub</c>). A caller lists their own
/// payments; <c>payments:read-all</c> lists everyone's. The list is ordered by a <see cref="DateTimeOffset"/>, which
/// SQLite can't translate (R-068), so the filter is tested on its query (<see cref="GetPayments.VisibleTo"/>), against
/// the module's SQLite database.
/// </summary>
public sealed class PaymentOwnershipTests(NovaCoreApiFactory factory) : IClassFixture<NovaCoreApiFactory>
{
    [Fact]
    public async Task A_payment_belongs_to_the_caller_who_created_it()
    {
        var productId = await factory.AddProductAsync(stock: 5);
        var subject = Guid.NewGuid().ToString();
        factory.GrantPermissions(subject, PaymentsPermissions.Create);
        using var client = factory.CreateClientWithClaims(new Claim(NovaClaims.Subject, subject));

        var response = await client.PostAsJsonAsync("/api/v1/payments", new { productId, quantity = 1 }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var paymentId = (await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken)).GetProperty("paymentId").GetGuid();
        await using var scope = factory.Services.CreateAsyncScope();
        Assert.Equal(subject, await scope.ServiceProvider.GetRequiredService<PaymentsDbContext>().Payments
            .Where(payment => payment.Id == paymentId)
            .Select(payment => payment.OwnerId)
            .SingleAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task A_caller_sees_only_their_own_payments()
    {
        var mine = await AddPaymentAsync("owner-a");
        var theirs = await AddPaymentAsync("owner-b");

        var visible = await VisibleToAsync(new Caller("owner-a"));

        Assert.Contains(mine, visible);
        Assert.DoesNotContain(theirs, visible);
    }

    [Fact]
    public async Task Read_all_sees_every_payment()
    {
        var first = await AddPaymentAsync("owner-c");
        var second = await AddPaymentAsync("owner-d");

        var visible = await VisibleToAsync(new Caller("auditor", PaymentsPermissions.ReadAll));

        Assert.Contains(first, visible);
        Assert.Contains(second, visible);
    }

    [Fact]
    public void Read_all_is_a_declared_permission()
    {
        Assert.Contains(new PaymentsPermissionProvider().GetPermissions(), permission => string.Equals(permission.Name, "payments:read-all", StringComparison.Ordinal));
    }

    private async Task<Guid> AddPaymentAsync(string ownerId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var payments = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        var payment = new Payment(ownerId, Guid.CreateVersion7(), "Widget", 1, 10m);

        // Not for the dispatcher: no product answers it here.
        payment.ClearDomainEvents();
        payments.Payments.Add(payment);
        await payments.SaveChangesAsync(TestContext.Current.CancellationToken);
        return payment.Id;
    }

    private async Task<List<Guid>> VisibleToAsync(ICurrentUser caller)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await GetPayments.VisibleTo(scope.ServiceProvider.GetRequiredService<PaymentsDbContext>().Payments, caller)
            .Select(payment => payment.Id)
            .ToListAsync(TestContext.Current.CancellationToken);
    }

    private sealed class Caller(string userId, params string[] permissions) : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public string? UserId => userId;
        public string? UserName => null;
        public string? Email => null;
        public IReadOnlyCollection<string> Roles => [];
        public IReadOnlyCollection<string> Permissions => permissions;
        public bool IsInRole(string role) => false;
        public bool HasPermission(string permission) => permissions.Contains(permission, StringComparer.Ordinal);
    }
}
