using Xunit;

namespace NovaCore.Api.Tests.IdentityServer;

/// <summary>
/// The identity provider tests start many hosts and hash passwords with PBKDF2, which loads the CPU. Run in parallel
/// with the rest, they made the startup race in <c>WebApplicationFactory</c> (docs/plan/RESEARCH.md R-055) hit most full
/// runs, so they run on their own, after the parallel tests.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class IdentityServerTestGroup
{
    public const string Name = "Identity provider";
}
