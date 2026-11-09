using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NovaCore.AspNetCore.Auth;

namespace NovaCore.Identity.OpenIddict;

/// <summary>
/// Invites a person to an account: the provider emails a link that sets the password and confirms the address. The
/// invitation is the account's only; a product that invites someone into a tenant adds the membership itself with the
/// returned subject (e.g. <c>TenantDirectory.AddMemberAsync(tenant, subject, roles)</c>), so the identity and
/// multi-tenancy add-ons don't know each other. Guard the endpoint that calls it with the product's own permission.
/// </summary>
/// <remarks>
/// A new address gets an account without a password that can't sign in until the invitation is accepted. An existing
/// account isn't changed: accepting the invitation of a confirmed account sets no password (the person signs in with
/// their own), so an invitation can't take over an account. The link works once, for <see cref="Lifetime"/>, and holds a
/// 256-bit random token the store keeps only as its SHA-256 hash. The email is queued, never awaited.
/// </remarks>
public sealed class AccountInvitations
{
    /// <summary>How long an invitation link works.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);

    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<ApplicationRole> _roles;
    private readonly IdentityServerDbContext _store;
    private readonly EmailQueue _emails;
    private readonly JwtOptions _jwt;
    private readonly IdentitySignInOptions _signIn;
    private readonly TimeProvider _timeProvider;
    private readonly SecurityAuditLogger _audit;

    internal AccountInvitations(
        UserManager<ApplicationUser> users,
        RoleManager<ApplicationRole> roles,
        IdentityServerDbContext store,
        EmailQueue emails,
        IOptions<JwtOptions> jwt,
        IOptions<IdentityServerOptions> identity,
        TimeProvider timeProvider,
        SecurityAuditLogger audit)
    {
        _users = users;
        _roles = roles;
        _store = store;
        _emails = emails;
        _jwt = jwt.Value;
        _signIn = identity.Value.SignIn;
        _timeProvider = timeProvider;
        _audit = audit;
    }

    /// <summary>
    /// Emails an invitation to <paramref name="email"/>, creating the account when the address has none, and returns the
    /// account's id, the <c>sub</c> of its tokens.
    /// </summary>
    /// <param name="email">The invited person's email address.</param>
    /// <param name="cancellationToken">Cancels saving the invitation.</param>
    /// <returns>The invited account's id.</returns>
    /// <exception cref="InvalidOperationException">The account couldn't be created, e.g. the address isn't valid.</exception>
    public async Task<Guid> InviteAsync(string email, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var user = await _users.FindByEmailAsync(email) ?? await CreateAccountAsync(email);

        var token = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
        var now = _timeProvider.GetUtcNow();
        _store.Invitations.Add(new Invitation
        {
            UserId = user.Id,
            TokenHash = Hash(token),
            CreatedAtUtc = now,
            ExpiresAtUtc = now + Lifetime
        });
        await _store.SaveChangesAsync(cancellationToken);

        var link = QueryHelpers.AddQueryString(
            new Uri(new Uri(_jwt.Authority.TrimEnd('/') + "/"), _signIn.InvitationPath.TrimStart('/')).AbsoluteUri, "token", token);
        // Queued, not sent here, like every email of the provider (EmailQueue).
        _emails.Enqueue(user.Email!, "You're invited",
            $"You have been invited to an account. <a href=\"{HtmlEncoder.Default.Encode(link)}\">Accept the invitation</a>. "
            + $"The link works once, for {Lifetime.TotalDays:0} days. If you didn't expect it, ignore this email.");
        _audit.InvitationCreated(user.Id);
        return user.Id;
    }

    /// <summary>The invitation behind <paramref name="token"/>, if it can still be accepted.</summary>
    internal async Task<(InvitationState State, Invitation? Invitation)> FindAsync(string token, CancellationToken cancellationToken)
    {
        var hash = Hash(token);
        var invitation = await _store.Invitations.Include(entry => entry.User)
            .SingleOrDefaultAsync(entry => entry.TokenHash == hash, cancellationToken);

        return invitation switch
        {
            null => (InvitationState.NotFound, null),
            { AcceptedAtUtc: not null } => (InvitationState.Used, invitation),
            _ when invitation.ExpiresAtUtc <= _timeProvider.GetUtcNow() => (InvitationState.Expired, invitation),
            { User.EmailConfirmed: true } => (InvitationState.AccountExists, invitation),
            _ => (InvitationState.Pending, invitation)
        };
    }

    /// <summary>
    /// Accepts the invitation: sets <paramref name="password"/> and confirms the address of an account that isn't
    /// confirmed yet. The invitation is marked used first, in one conditional update, so two requests with one link
    /// accept it once.
    /// </summary>
    internal async Task<(InvitationState State, IReadOnlyList<string> Errors)> AcceptAsync(
        string token, string password, CancellationToken cancellationToken)
    {
        var (state, invitation) = await FindAsync(token, cancellationToken);
        if (state != InvitationState.Pending)
            return (state, []);

        var user = invitation!.User;
        var errors = new List<string>();
        foreach (var validator in _users.PasswordValidators)
            errors.AddRange((await validator.ValidateAsync(_users, user, password)).Errors.Select(error => error.Description));
        if (errors.Count > 0)
            return (InvitationState.PasswordRejected, errors);

        var now = _timeProvider.GetUtcNow();
        var marked = await _store.Invitations
            .Where(entry => entry.Id == invitation.Id && entry.AcceptedAtUtc == null)
            .ExecuteUpdateAsync(set => set.SetProperty(entry => entry.AcceptedAtUtc, now), cancellationToken);
        if (marked == 0)
            return (InvitationState.Used, []);

        // An unconfirmed account that registered with a password gets the new one: holding the link is holding the mailbox.
        var setPassword = await _users.HasPasswordAsync(user)
            ? await _users.ResetPasswordAsync(user, await _users.GeneratePasswordResetTokenAsync(user), password)
            : await _users.AddPasswordAsync(user, password);
        if (!setPassword.Succeeded)
            return (InvitationState.PasswordRejected, setPassword.Errors.Select(error => error.Description).ToList());

        var confirmed = await _users.ConfirmEmailAsync(user, await _users.GenerateEmailConfirmationTokenAsync(user));
        if (!confirmed.Succeeded)
            throw new InvalidOperationException("Confirming the invited account's email failed: "
                                                + string.Join(", ", confirmed.Errors.Select(error => error.Code)));

        _audit.InvitationAccepted(user.Id);
        return (InvitationState.Accepted, []);
    }

    private async Task<ApplicationUser> CreateAccountAsync(string email)
    {
        var user = new ApplicationUser { UserName = email, Email = email, CreatedAtUtc = _timeProvider.GetUtcNow() };
        var created = await _users.CreateAsync(user);
        if (!created.Succeeded)
            throw new InvalidOperationException("The invited account couldn't be created: "
                                                + string.Join(", ", created.Errors.Select(error => error.Code)));

        // Like registration: the seed's User role when it exists (AccountEndpoints.Register).
        if (await _roles.RoleExistsAsync(IdentityRoles.User))
            await _users.AddToRoleAsync(user, IdentityRoles.User);
        return user;
    }

    private static string Hash(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}

/// <summary>Where an invitation stands, or what accepting it decided.</summary>
internal enum InvitationState
{
    Pending,
    NotFound,
    Used,
    Expired,

    // A confirmed account: the invitation sets no password (D-091 (5)).
    AccountExists,

    // The password breaks the policy; the invitation stays unused.
    PasswordRejected,
    Accepted
}
