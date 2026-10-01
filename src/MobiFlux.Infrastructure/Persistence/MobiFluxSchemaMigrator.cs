using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace MobiFlux.Infrastructure.Persistence;

public static class MobiFluxSchemaMigrator
{
    private sealed record SchemaMigration(string Id, string Sql);

    private static readonly IReadOnlyList<SchemaMigration> Migrations =
    [
        new SchemaMigration("202609260001_AddProxySessions", """
            CREATE TABLE IF NOT EXISTS "proxy_sessions" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_proxy_sessions" PRIMARY KEY,
                "EndpointId" TEXT NOT NULL,
                "DeviceId" TEXT NOT NULL,
                "DestinationHost" TEXT NOT NULL,
                "DestinationPort" INTEGER NOT NULL,
                "StartedAt" TEXT NOT NULL,
                "EndedAt" TEXT NOT NULL,
                "BytesUp" INTEGER NOT NULL,
                "BytesDown" INTEGER NOT NULL,
                "CloseReason" TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS "IX_proxy_sessions_EndedAt_EndpointId" ON "proxy_sessions" ("EndedAt", "EndpointId");
            CREATE INDEX IF NOT EXISTS "IX_proxy_sessions_EndedAt_DeviceId" ON "proxy_sessions" ("EndedAt", "DeviceId");
            """)
        ,
        new SchemaMigration("202609260002_NormalizeProxySessionTimestamps", """
            ALTER TABLE "proxy_sessions" RENAME TO "proxy_sessions_legacy";
            CREATE TABLE "proxy_sessions" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_proxy_sessions" PRIMARY KEY,
                "EndpointId" TEXT NOT NULL,
                "DeviceId" TEXT NOT NULL,
                "DestinationHost" TEXT NOT NULL,
                "DestinationPort" INTEGER NOT NULL,
                "StartedAt" INTEGER NOT NULL,
                "EndedAt" INTEGER NOT NULL,
                "BytesUp" INTEGER NOT NULL,
                "BytesDown" INTEGER NOT NULL,
                "CloseReason" TEXT NOT NULL
            );
            INSERT INTO "proxy_sessions" ("Id", "EndpointId", "DeviceId", "DestinationHost", "DestinationPort", "StartedAt", "EndedAt", "BytesUp", "BytesDown", "CloseReason")
            SELECT "Id", "EndpointId", "DeviceId", "DestinationHost", "DestinationPort",
                   CAST((julianday("StartedAt") - 2440587.5) * 86400000 AS INTEGER),
                   CAST((julianday("EndedAt") - 2440587.5) * 86400000 AS INTEGER),
                   "BytesUp", "BytesDown", "CloseReason"
            FROM "proxy_sessions_legacy";
            DROP TABLE "proxy_sessions_legacy";
            CREATE INDEX "IX_proxy_sessions_EndedAt_EndpointId" ON "proxy_sessions" ("EndedAt", "EndpointId");
            CREATE INDEX "IX_proxy_sessions_EndedAt_DeviceId" ON "proxy_sessions" ("EndedAt", "DeviceId");
            """)
    ];

    public static async Task ApplyPendingMobiFluxSchemaMigrationsAsync(this MobiFluxDbContext database, CancellationToken cancellationToken = default)
    {
        await database.Database.EnsureCreatedAsync(cancellationToken);
        var connection = database.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);
        try
        {
            await ExecuteAsync(connection, "CREATE TABLE IF NOT EXISTS \"mobiflux_schema_versions\" (\"Id\" TEXT NOT NULL CONSTRAINT \"PK_mobiflux_schema_versions\" PRIMARY KEY, \"AppliedAt\" TEXT NOT NULL);", cancellationToken);
            var applied = await LoadAppliedIdsAsync(connection, cancellationToken);
            foreach (var migration in Migrations.Where(migration => !applied.Contains(migration.Id)))
            {
                await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
                await ExecuteAsync(connection, migration.Sql, cancellationToken, transaction);
                await ExecuteAsync(connection, "INSERT INTO \"mobiflux_schema_versions\" (\"Id\", \"AppliedAt\") VALUES ($id, $appliedAt);", cancellationToken, transaction, ("$id", migration.Id), ("$appliedAt", DateTimeOffset.UtcNow.ToString("O")));
                await transaction.CommitAsync(cancellationToken);
            }
        }
        finally { await connection.CloseAsync(); }
    }

    private static async Task<HashSet<string>> LoadAppliedIdsAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT \"Id\" FROM \"mobiflux_schema_versions\";";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        while (await reader.ReadAsync(cancellationToken)) ids.Add(reader.GetString(0));
        return ids;
    }

    private static async Task ExecuteAsync(DbConnection connection, string sql, CancellationToken cancellationToken, DbTransaction? transaction = null, params (string Name, object Value)[] parameters)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = transaction;
        foreach (var (name, value) in parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;
            command.Parameters.Add(parameter);
        }
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
