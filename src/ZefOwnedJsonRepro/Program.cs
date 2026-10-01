using Microsoft.EntityFrameworkCore;
using ZefOwnedJsonRepro;

await using (var setup = ReproDbContext.Create())
{
    await setup.Database.EnsureDeletedAsync();
    await setup.Database.EnsureCreatedAsync();
}

await Run("EF Core SaveChanges", async (context, team) =>
{
    context.Teams.Add(team);
    await context.SaveChangesAsync();
});
await Run("ZEF BulkInsert", (context, team) => context.BulkInsertAsync(new[] { team }));
await Run("ZEF BulkMerge", (context, team) => context.BulkMergeAsync(new[] { team }));

static async Task Run(string label, Func<ReproDbContext, Team, Task> write)
{
    var team = ReproDbContext.NewTeam(label);
    await using (var context = ReproDbContext.Create())
        await write(context, team);

    await using var reader = ReproDbContext.Create();
    Console.WriteLine($"=== {label}");
    Console.WriteLine($"jsonb: {await reader.ReadMembersJsonAsync(team.Id)}");
    try
    {
        var loaded = await reader.Teams.AsNoTracking().SingleAsync(x => x.Id == team.Id);
        Console.WriteLine($"read back OK, TagIds: {loaded.Members[0].TagIds.Count}");
    }
    catch (Exception e)
    {
        Console.WriteLine($"read back FAILED: {e.GetType().FullName}: {e.Message}");
    }
    Console.WriteLine();
}
