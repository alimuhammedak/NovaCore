using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NovaCore.Abstractions.Results;
using NovaCore.AspNetCore.Authorization;
using NovaCore.AspNetCore.Http;

namespace NovaCore.Identity.OpenIddict;

/// <summary>
/// The invitation endpoints (docs/plan/DECISIONS.md D-091 (5), D-095): a project's screen reads an invitation's state and
/// accepts it; the platform's administrators invite with <c>identity:invitations:create</c>. A product that invites into
/// a tenant builds its own endpoint on <see cref="AccountInvitations"/> with its own permission. The token travels in the
/// body, never in the path, so request logs don't hold it.
/// </summary>
internal static class InvitationEndpoints
{
    private static readonly Error NotFound = Error.NotFound("invitation.not_found", "The invitation link is not valid.");

    private static readonly Error Used = Error.Conflict("invitation.used", "The invitation has already been accepted.");

    private static readonly Error AccountExists =
        Error.Conflict("invitation.account_exists", "The account already exists. Sign in with its password.");

    private static readonly AccountEndpoints.MessageResponse Accepted =
        new("The invitation has been accepted. Sign in with the new password.");

    /// <summary>The invited address and until when the link works.</summary>
    internal sealed record InvitationStatusResponse(string Email, DateTimeOffset ExpiresAtUtc);

    /// <summary>The invited account's id: the <c>sub</c> of its tokens, for a membership.</summary>
    internal sealed record InvitationCreatedResponse(Guid UserId);

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var invitations = endpoints.MapGroup("/api/v1/auth/invitations").WithTags("Auth").RequireRateLimiting("strict")
            .AllowAnonymous().WithMetadata(new DisableCorsAttribute());

        invitations.MapPost("/status", Status)
            .WithName("InvitationStatus")
            .WithSummary("Read an invitation: the invited address, or why it can't be accepted")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status410Gone);

        invitations.MapPost("/accept", Accept)
            .WithName("InvitationAccept")
            .WithSummary("Accept an invitation: set the password and confirm the address (signs nobody in)")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status410Gone);

        endpoints.MapGroup("/api/v1/admin").WithTags("Admin")
            .MapPost("/invitations", Create)
            .WithName("AdminCreateInvitation")
            .WithSummary("Invite a person to an account (creates it when the address has none; changes no existing account)")
            .RequirePermission(IdentityPermissions.InvitationsCreate);
    }

    private static async Task<Results<Ok<InvitationStatusResponse>, ProblemHttpResult>> Status(
        InvitationTokenRequest request, AccountInvitations invitations, CancellationToken cancellationToken)
    {
        var (state, invitation) = await invitations.FindAsync(request.Token, cancellationToken);
        return state == InvitationState.Pending
            ? TypedResults.Ok(new InvitationStatusResponse(invitation!.User.Email!, invitation.ExpiresAtUtc))
            : ProblemFor(state);
    }

    private static async Task<Results<Ok<AccountEndpoints.MessageResponse>, ValidationProblem, ProblemHttpResult>> Accept(
        InvitationAcceptRequest request, AccountInvitations invitations, CancellationToken cancellationToken)
    {
        var (state, errors) = await invitations.AcceptAsync(request.Token, request.Password, cancellationToken);
        return state switch
        {
            InvitationState.Accepted => TypedResults.Ok(Accepted),
            InvitationState.PasswordRejected => TypedResults.ValidationProblem(
                new Dictionary<string, string[]>(StringComparer.Ordinal) { ["password"] = [.. errors] }),
            _ => ProblemFor(state)
        };
    }

    private static async Task<Ok<InvitationCreatedResponse>> Create(
        InvitationCreateRequest request, AccountInvitations invitations, CancellationToken cancellationToken) =>
        TypedResults.Ok(new InvitationCreatedResponse(await invitations.InviteAsync(request.Email, cancellationToken)));

    private static ProblemHttpResult ProblemFor(InvitationState state) => state switch
    {
        InvitationState.Used => ResultHttpExtensions.Problem(Used),
        InvitationState.AccountExists => ResultHttpExtensions.Problem(AccountExists),
        // The core's errors have no 410; the same ProblemDetails shape as ResultHttpExtensions.Problem writes.
        InvitationState.Expired => TypedResults.Problem(
            detail: "The invitation has expired. Ask for a new one.",
            statusCode: StatusCodes.Status410Gone,
            title: "Gone",
            type: $"https://httpstatuses.io/{StatusCodes.Status410Gone}",
            extensions: new Dictionary<string, object?>(StringComparer.Ordinal) { ["code"] = "invitation.expired" }),
        _ => ResultHttpExtensions.Problem(NotFound)
    };
}
