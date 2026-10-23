using System.Text.RegularExpressions;
using NovaCore.Api.Tests.SqlServer;
using Xunit;

namespace NovaCore.Api.Tests.Architecture;

/// <summary>
/// The SQL Server tests run the image docker-compose.yml runs, digest included. Dependabot updates the compose file only,
/// so this turns red until the test image follows it.
/// </summary>
public partial class SqlServerImageTests
{
    [Fact]
    public void The_test_server_runs_the_compose_files_image()
    {
        var compose = File.ReadAllText(Path.Combine(Solution.RootDirectory, "docker-compose.yml"));

        var images = ComposeImage().Matches(compose).Select(match => match.Groups["image"].Value).ToList();

        Assert.Equal([TestSqlServer.Image], images);
    }

    [GeneratedRegex(@"^\s*image:\s*(?<image>mcr\.microsoft\.com/mssql/server\S*)\s*$", RegexOptions.Multiline)]
    private static partial Regex ComposeImage();
}
