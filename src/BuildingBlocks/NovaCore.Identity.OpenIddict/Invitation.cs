namespace NovaCore.Identity.OpenIddict;

/// <summary>
/// An account invitation (docs/plan/DECISIONS.md D-091 (5)): the emailed link's token, kept as its SHA-256 hash, for the
/// account it invites. It is accepted once, within <see cref="AccountInvitations.Lifetime"/>.
/// </summary>
internal sealed class Invitation
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public required Guid UserId { get; init; }

    /// <summary>The lower-case hex SHA-256 of the token: a 256-bit random token needs no slow hash (D-091 (5)).</summary>
    public required string TokenHash { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public required DateTimeOffset ExpiresAtUtc { get; init; }

    public DateTimeOffset? AcceptedAtUtc { get; set; }

    public ApplicationUser User { get; init; } = null!;
}
