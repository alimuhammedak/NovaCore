using System.Text.RegularExpressions;
using NovaCore.Api.Tests.PostgreSql;
using NovaCore.Api.Tests.SqlServer;
using Xunit;

namespace NovaCore.Api.Tests.Architecture;

/// <summary>
/// The database tests run the images docker-compose.yml runs, digests included. Dependabot updates the compose file only,
/// so this turns red until the test image follows it.
/// </summary>
public partial class DatabaseImageTests
{
    private static readonly string Compose = File.ReadAllText(Path.Combine(Solution.RootDirectory, "docker-compose.yml"));

    [Fact]
    public void The_test_SQL_Server_runs_the_compose_files_image() =>
        Assert.Equal([TestSqlServer.Image], Images(SqlServerImage()));

    [Fact]
    public void The_test_PostgreSQL_runs_the_compose_files_image() =>
        Assert.Equal([TestPostgreSql.Image], Images(PostgreSqlImage()));

    private static List<string> Images(Regex image) =>
        image.Matches(Compose).Select(match => match.Groups["image"].Value).ToList();

    [GeneratedRegex(@"^\s*image:\s*(?<image>mcr\.microsoft\.com/mssql/server\S*)\s*$", RegexOptions.Multiline, matchTimeoutMilliseconds: 1000)]
    private static partial Regex SqlServerImage();

    [GeneratedRegex(@"^\s*image:\s*(?<image>postgres:\S*)\s*$", RegexOptions.Multiline, matchTimeoutMilliseconds: 1000)]
    private static partial Regex PostgreSqlImage();
}
