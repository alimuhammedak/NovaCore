using System.Text.RegularExpressions;
using Xunit;

namespace NovaCore.Api.Tests.Architecture;

/// <summary>
/// GitHub Actions are referenced by full commit SHA (DECISIONS D-051 (1)). Dependabot updates the repository's own
/// workflows, but it can't parse the template's: that file only becomes YAML once the template engine has removed its
/// <c>#if</c> lines. So every action must be pinned to the same commit everywhere, and a Dependabot update fails here
/// until the template's workflow follows it (D-051 (7)).
/// </summary>
public class WorkflowPinTests
{
    // `uses: owner/repo[/path]@ref` in a step; a trailing "# vX.Y.Z" comment names the release the SHA came from.
    private static readonly Regex Uses = new(@"^\s*(-\s*)?uses:\s*(?<action>[^@\s]+)@(?<ref>[^\s#]+)");
    private static readonly Regex FullSha = new("^[0-9a-f]{40}$");

    private static readonly string[] WorkflowDirectories =
    [
        Path.Combine(".github", "workflows"),
        Path.Combine("src", "Templates", "NovaCore.Templates", "content", "novacore", ".github", "workflows")
    ];

    [Fact]
    public void Every_action_is_pinned_to_one_full_commit_sha()
    {
        var uses = AllUses().ToList();
        Assert.NotEmpty(uses);

        var problems =
            from use in uses
            group use by use.Action into action
            let refs = action.Select(use => use.Ref).Distinct().ToList()
            where refs.Count > 1 || !FullSha.IsMatch(refs[0])
            select $"{action.Key}: {string.Join(", ", action.Select(use => $"{use.Ref} ({use.Location})"))}";

        Assert.Empty(problems);
    }

    private static IEnumerable<(string Action, string Ref, string Location)> AllUses() =>
        from directory in WorkflowDirectories
        from path in Directory.EnumerateFiles(Path.Combine(Solution.RootDirectory, directory), "*.yml")
        from line in File.ReadLines(path).Select((text, index) => (text, number: index + 1))
        let match = Uses.Match(line.text)
        where match.Success
        select (match.Groups["action"].Value, match.Groups["ref"].Value,
            $"{Path.GetRelativePath(Solution.RootDirectory, path)}:{line.number}");
}
