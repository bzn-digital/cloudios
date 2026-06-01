using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Bzn.Cloudios.Infrastructure.Persistence;

public sealed class SqlitePragmaInterceptor : DbConnectionInterceptor
{
    private static readonly string[] Pragmas =
    [
        "PRAGMA journal_mode=WAL;",
        "PRAGMA synchronous=NORMAL;",
        "PRAGMA busy_timeout=5000;",
        "PRAGMA cache_size=-64000;",
        "PRAGMA temp_store=MEMORY;",
        "PRAGMA foreign_keys=ON;"
    ];

    private bool _pragmasApplied;

    public override InterceptionResult ConnectionOpening(
        System.Data.Common.DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result)
    {
        ApplyPragmas(connection);
        return result;
    }

    public override ValueTask<InterceptionResult> ConnectionOpeningAsync(
        System.Data.Common.DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result,
        CancellationToken cancellationToken = default)
    {
        ApplyPragmas(connection);
        return new ValueTask<InterceptionResult>(result);
    }

    private void ApplyPragmas(System.Data.Common.DbConnection? connection)
    {
        if (connection is null || _pragmasApplied) return;

        using var cmd = connection.CreateCommand();
        foreach (var pragma in Pragmas)
        {
            cmd.CommandText = pragma;
            cmd.ExecuteNonQuery();
        }

        _pragmasApplied = true;
    }
}
