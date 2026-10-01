using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ZefOwnedJsonRepro;

namespace ZefOwnedJsonRepro.Tests;

// Every test runs once per writer. EF Core SaveChanges is the reference and passes;
// the ZEF bulk operations write the same entity and fail.
[Collection(DatabaseCollection.Name)]
public class PrimitiveCollectionInOwnedJsonTests(DatabaseFixture database)
{
    public static TheoryData<Writer> Writers => new(Enum.GetValues<Writer>());

    [Theory]
    [MemberData(nameof(Writers))]
    public async Task Primitive_collection_is_stored_as_json_array(Writer writer)
    {
        var team = ReproDbContext.NewTeam();
        await WriteAsync(writer, team);

        var member = await ReadStoredMemberAsync(team.Id);

        var tagIds = member.GetProperty(nameof(TeamMember.TagIds));
        Assert.Equal(JsonValueKind.Array, tagIds.ValueKind);
        Assert.Equal(
            team.Members[0].TagIds.Select(x => x.ToString()),
            tagIds.EnumerateArray().Select(x => x.GetString()));
    }

    [Theory]
    [MemberData(nameof(Writers))]
    public async Task Written_entity_can_be_read_back_by_ef_core(Writer writer)
    {
        var team = ReproDbContext.NewTeam();
        await WriteAsync(writer, team);

        await using var context = database.CreateContext();
        var loaded = await context.Teams.AsNoTracking().SingleAsync(x => x.Id == team.Id);

        Assert.Equal(team.Members[0].TagIds, Assert.Single(loaded.Members).TagIds);
    }

    // Control: scalar members of the same owned JSON type are written correctly by every writer,
    // so only the primitive collection is affected.
    [Theory]
    [MemberData(nameof(Writers))]
    public async Task Scalar_members_are_stored_correctly(Writer writer)
    {
        var team = ReproDbContext.NewTeam();
        await WriteAsync(writer, team);

        var member = await ReadStoredMemberAsync(team.Id);
        var expected = team.Members[0];

        Assert.Equal(expected.PersonId, member.GetProperty(nameof(TeamMember.PersonId)).GetGuid());
        Assert.Equal(expected.Share, member.GetProperty(nameof(TeamMember.Share)).GetDecimal());
        Assert.Equal(expected.Role.ToString(), member.GetProperty(nameof(TeamMember.Role)).GetString());
        Assert.Equal(expected.JoinedAt, member.GetProperty(nameof(TeamMember.JoinedAt)).GetDateTimeOffset());
    }

    private async Task WriteAsync(Writer writer, Team team)
    {
        await using var context = database.CreateContext();
        await writer.WriteAsync(context, team);
    }

    private async Task<JsonElement> ReadStoredMemberAsync(Guid teamId)
    {
        await using var context = database.CreateContext();
        using var document = JsonDocument.Parse(await context.ReadMembersJsonAsync(teamId));
        return Assert.Single(document.RootElement.EnumerateArray()).Clone();
    }
}
