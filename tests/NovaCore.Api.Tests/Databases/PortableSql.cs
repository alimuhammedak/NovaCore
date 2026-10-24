using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace NovaCore.Api.Tests.Databases;

/// <summary>
/// Names in raw SQL that every provider reads: a table of the context's schema and a column, quoted by the context's own
/// provider (<c>[catalog].[outbox_messages]</c> on SQL Server, <c>"catalog"."outbox_messages"</c> on PostgreSQL, where an
/// unquoted name folds to lower case). Values go as parameters, never as literals (<c>N'...'</c> is SQL Server's).
/// </summary>
internal sealed class PortableSql(DbContext context)
{
    private readonly ISqlGenerationHelper _sql = context.GetService<ISqlGenerationHelper>();
    private readonly string? _schema = context.Model.GetDefaultSchema();

    public string Table(string name) => _sql.DelimitIdentifier(name, _schema);

    public string Column(string name) => _sql.DelimitIdentifier(name);
}
