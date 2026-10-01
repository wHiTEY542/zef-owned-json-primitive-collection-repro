using ZefOwnedJsonRepro;

namespace ZefOwnedJsonRepro.Tests;

public sealed class DatabaseFixture : IAsyncLifetime
{
    public const string DatabaseName = "zef_repro_tests";

    public async Task InitializeAsync()
    {
        await using var context = ReproDbContext.Create(DatabaseName);
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public ReproDbContext CreateContext() => ReproDbContext.Create(DatabaseName);
}

[CollectionDefinition(Name)]
public sealed class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    public const string Name = "database";
}
