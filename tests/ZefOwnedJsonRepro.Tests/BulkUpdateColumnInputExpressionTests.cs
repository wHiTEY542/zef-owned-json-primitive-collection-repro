using Microsoft.EntityFrameworkCore;
using ZefOwnedJsonRepro;

namespace ZefOwnedJsonRepro.Tests;

// "Possibly related" part of the issue: updating only a plain column of an entity that owns a
// ToJson() navigation. EF Core ExecuteUpdate is the reference; ZEF BulkUpdate does the same.
[Collection(DatabaseCollection.Name)]
public class BulkUpdateColumnInputExpressionTests(DatabaseFixture database)
{
    [Fact]
    public async Task EfCore_ExecuteUpdate_updates_plain_column()
    {
        var team = await InsertTeamAsync();

        await using (var context = database.CreateContext())
            await context.Teams
                .Where(x => x.Id == team.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Name, "Renamed"));

        Assert.Equal("Renamed", await ReadNameAsync(team.Id));
    }

    [Fact]
    public async Task Zef_BulkUpdate_with_ColumnInputExpression_updates_plain_column()
    {
        var team = await InsertTeamAsync();
        team.Name = "Renamed";

        await using (var context = database.CreateContext())
            await context.BulkUpdateAsync(new[] { team }, options =>
                options.ColumnInputExpression = x => new { x.Id, x.Name });

        Assert.Equal("Renamed", await ReadNameAsync(team.Id));
    }

    private async Task<Team> InsertTeamAsync()
    {
        var team = ReproDbContext.NewTeam();
        await using var context = database.CreateContext();
        context.Teams.Add(team);
        await context.SaveChangesAsync();
        return team;
    }

    private async Task<string> ReadNameAsync(Guid teamId)
    {
        await using var context = database.CreateContext();
        return await context.Teams.Where(x => x.Id == teamId).Select(x => x.Name).SingleAsync();
    }
}
