using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ZefOwnedJsonRepro;

public class ReproDbContext(string connectionString) : DbContext
{
    public const string DefaultConnectionString =
        "Host=localhost;Port=5690;Username=postgres;Password=postgres;Database=zef_repro";

    public DbSet<Team> Teams => Set<Team>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.UseNpgsql(connectionString);

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<Team>().OwnsMany(x => x.Members, b =>
        {
            b.ToJson();
            b.Property(x => x.Role).HasConversion<string>();
        });

    public static ReproDbContext Create(string? database = null)
    {
        var connectionString = Environment.GetEnvironmentVariable("REPRO_CONNECTION_STRING")
                               ?? DefaultConnectionString;
        if (database is not null)
            connectionString = new NpgsqlConnectionStringBuilder(connectionString) { Database = database }.ConnectionString;
        return new ReproDbContext(connectionString);
    }

    public async Task<string> ReadMembersJsonAsync(Guid teamId)
    {
        var connection = Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """SELECT "Members"::text FROM "Teams" WHERE "Id" = @id""";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "id";
        parameter.Value = teamId;
        command.Parameters.Add(parameter);

        return (string)(await command.ExecuteScalarAsync())!;
    }

    public static Team NewTeam(string name = "Team A") => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Members =
        [
            new TeamMember
            {
                PersonId = Guid.NewGuid(),
                Share = 0.5m,
                Role = MemberRole.Lead,
                JoinedAt = new DateTimeOffset(2025, 1, 2, 3, 4, 5, TimeSpan.Zero),
                TagIds = [Guid.NewGuid(), Guid.NewGuid()]
            }
        ]
    };
}
