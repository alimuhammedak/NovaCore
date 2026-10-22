using NovaCore.Api.Tests.Architecture;
using Xunit;

namespace NovaCore.Api.Tests.Integration;

/// <summary>
/// The API contract (PLAN F6c.5): the OpenAPI document the host serves is committed at <c>docs/openapi/v1.json</c>, and CI
/// compares it with the base revision's for breaking changes (<c>eng/check-openapi.sh</c>, oasdiff). This test keeps the
/// file current: when the served document differs, it writes it there and fails, so the change shows in the diff and is
/// committed with the code that made it. The document comes from the test host (Development): .NET 10's build-time
/// generation runs Program in Production, where the host doesn't start without a production's keys and stores
/// (docs/plan/DECISIONS.md D-061).
/// </summary>
public sealed class OpenApiContractTests(NovaCoreApiFactory factory) : IClassFixture<NovaCoreApiFactory>
{
    private static readonly string CommittedDocument = Path.Combine(Solution.RootDirectory, "docs", "openapi", "v1.json");

    [Fact]
    public async Task The_committed_document_is_the_one_the_host_serves()
    {
        using var client = factory.CreateClient();
        var served = (await client.GetStringAsync("/openapi/v1.json", TestContext.Current.CancellationToken))
            .ReplaceLineEndings("\n");
        var committed = File.Exists(CommittedDocument)
            ? (await File.ReadAllTextAsync(CommittedDocument, TestContext.Current.CancellationToken)).ReplaceLineEndings("\n")
            : null;

        if (served == committed)
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(CommittedDocument)!);
        await File.WriteAllTextAsync(CommittedDocument, served, TestContext.Current.CancellationToken);
        Assert.Fail($"The API's OpenAPI document changed, and {CommittedDocument} now holds the new one. Review its diff "
                    + "(bash eng/check-openapi.sh lists the breaking changes) and commit it with the change.");
    }
}
