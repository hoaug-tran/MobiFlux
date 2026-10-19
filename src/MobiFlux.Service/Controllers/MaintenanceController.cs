using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MobiFlux.Infrastructure.Persistence;

namespace MobiFlux.Service.Controllers;

[ApiController]
[Route("api/v1/mobiflux/maintenance")]
public sealed class MaintenanceController(MobiFluxDbContext database) : ControllerBase
{
    private static readonly SemaphoreSlim BackupGate = new(1, 1);

    [HttpPost("backup")]
    public async Task<IActionResult> Backup(CancellationToken cancellationToken)
    {
        if (!await BackupGate.WaitAsync(0, cancellationToken))
            throw new InvalidOperationException("Đang tạo hoặc tải bản sao lưu khác. Chờ hoàn tất rồi thử lại.");
        var temporary = Path.Combine(Path.GetTempPath(), $"mobiflux-backup-{Guid.NewGuid():N}.db");
        try
        {
            await database.Database.OpenConnectionAsync(cancellationToken);
            await using (var destination = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = temporary, Pooling = false }.ToString()))
            {
                await destination.OpenAsync(cancellationToken);
                ((SqliteConnection)database.Database.GetDbConnection()).BackupDatabase(destination);
            }
            Response.Headers.CacheControl = "no-store";
            Response.Headers.ContentDisposition = $"attachment; filename=\"mobiflux-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.db\"";
            Response.ContentType = "application/vnd.sqlite3";
            await using var contents = new FileStream(temporary, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.Asynchronous | FileOptions.SequentialScan);
            Response.ContentLength = contents.Length;
            await contents.CopyToAsync(Response.Body, cancellationToken);
            return new EmptyResult();
        }
        finally
        {
            try { if (System.IO.File.Exists(temporary)) System.IO.File.Delete(temporary); }
            finally { BackupGate.Release(); }
        }
    }
}
