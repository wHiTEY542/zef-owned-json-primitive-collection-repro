using Microsoft.EntityFrameworkCore;
using ZefOwnedJsonRepro;

namespace ZefOwnedJsonRepro.Tests;

public enum Writer
{
    EfCoreSaveChanges,
    ZefBulkInsert,
    ZefBulkMerge
}

public static class WriterExtensions
{
    public static async Task WriteAsync(this Writer writer, ReproDbContext context, Team team)
    {
        switch (writer)
        {
            case Writer.EfCoreSaveChanges:
                context.Teams.Add(team);
                await context.SaveChangesAsync();
                break;
            case Writer.ZefBulkInsert:
                await context.BulkInsertAsync(new[] { team });
                break;
            case Writer.ZefBulkMerge:
                await context.BulkMergeAsync(new[] { team });
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(writer), writer, null);
        }
    }
}
