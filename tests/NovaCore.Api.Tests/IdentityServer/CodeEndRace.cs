using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace NovaCore.Api.Tests.IdentityServer;

/// <summary>
/// Plays a request writing a reset code token (a /forgot issuing a new code) between a request's read of the code and
/// its end of that code. The code ends with a bulk <c>DELETE</c> (<c>ExecuteDeleteAsync</c>, not a save), so this is a
/// command interceptor: once given a competitor, the next bulk <c>DELETE</c> of user tokens first runs it to its end.
/// </summary>
public sealed class CodeEndRace : DbCommandInterceptor
{
    private readonly Lock _gate = new();
    private Func<Task>? _competitor;

    /// <summary>Runs <paramref name="competitor"/> (once) to its end before the next bulk delete of user tokens.</summary>
    public void RunBeforeNextTokenDelete(Func<Task> competitor)
    {
        lock (_gate)
            _competitor = competitor;
    }

    public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (Take(command) is { } competitor)
            await competitor();

        return await base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    private Func<Task>? Take(DbCommand command)
    {
        lock (_gate)
        {
            if (_competitor is null
                || !command.CommandText.Contains("DELETE", StringComparison.OrdinalIgnoreCase)
                || !command.CommandText.Contains("AspNetUserTokens", StringComparison.Ordinal))
                return null;

            var competitor = _competitor;
            _competitor = null;
            return competitor;
        }
    }
}
