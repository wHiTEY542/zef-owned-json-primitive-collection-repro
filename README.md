# ZEF repro: primitive collections inside owned `ToJson()` entities are double-encoded (PostgreSQL)

Minimal reproduction for Z.EntityFramework.Extensions.EFCore.

**Versions:** Z.EntityFramework.Extensions.EFCore 10.105.8.1, EF Core 10.0.12,
Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3, PostgreSQL 15.3, .NET 10

## Run

```bash
docker compose up -d --wait     # PostgreSQL 15.3 on localhost:5690
dotnet run --project src/ZefOwnedJsonRepro
dotnet test
```

Override the connection with `REPRO_CONNECTION_STRING` if needed.

## Model

```csharp
public class Team
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public List<TeamMember> Members { get; set; } = new();
}

public class TeamMember
{
    public Guid? PersonId { get; set; }
    public decimal Share { get; set; }
    public MemberRole Role { get; set; }           // HasConversion<string>()
    public DateTimeOffset JoinedAt { get; set; }
    public List<Guid> TagIds { get; set; } = new(); // primitive collection inside the JSON document
}

modelBuilder.Entity<Team>().OwnsMany(x => x.Members, b =>
{
    b.ToJson();
    b.Property(x => x.Role).HasConversion<string>();
});
```

## Console output

```
=== EF Core SaveChanges
jsonb: [{"Role": "Lead", "Share": 0.5, "TagIds": ["04f24d06-...", "a58408f6-..."], "JoinedAt": "2025-01-02T03:04:05+00:00", "PersonId": "33aaaa81-..."}]
read back OK, TagIds: 2

=== ZEF BulkInsert
jsonb: [{"Role": "Lead", "Share": 0.5, "TagIds": "[\"025c1a51-...\",\"1565127c-...\"]", "JoinedAt": "2025-01-02T03:04:05+00:00", "PersonId": "925c0da9-..."}]
read back FAILED: System.InvalidOperationException: Invalid token type: 'String'.

=== ZEF BulkMerge
jsonb: [{"Role": "Lead", "Share": 0.5, "TagIds": "[\"99cc50ac-...\",\"df81c893-...\"]", "JoinedAt": "2025-01-02T03:04:05+00:00", "PersonId": "dae64201-..."}]
read back FAILED: System.InvalidOperationException: Invalid token type: 'String'.
```

`BulkInsert`/`BulkMerge` serialize `TagIds` as a JSON **string** containing an array (double-encoded)
instead of a JSON array. EF Core then cannot materialize the row
(`JsonCollectionOfStructsReaderWriter<,>.FromJsonTyped` throws `Invalid token type: 'String'`).
Scalar members of the same owned type (Guid, decimal, string-converted enum, DateTimeOffset) are written correctly.

## Tests

Every test in `PrimitiveCollectionInOwnedJsonTests` runs once per writer: EF Core `SaveChanges`
(reference), ZEF `BulkInsert`, ZEF `BulkMerge`.

| Test | SaveChanges | BulkInsert | BulkMerge |
|---|---|---|---|
| `Primitive_collection_is_stored_as_json_array` | pass | **fail** (Expected Array, Actual String) | **fail** |
| `Written_entity_can_be_read_back_by_ef_core` | pass | **fail** (`Invalid token type: 'String'`) | **fail** |
| `Scalar_members_are_stored_correctly` (control) | pass | pass | pass |

### Possibly related

`BulkUpdateColumnInputExpressionTests`: updating only `Name` on the same entity.

| Test | Result |
|---|---|
| `EfCore_ExecuteUpdate_updates_plain_column` | pass |
| `Zef_BulkUpdate_with_ColumnInputExpression_updates_plain_column` | **fail** |

```csharp
await context.BulkUpdateAsync(new[] { team }, o => o.ColumnInputExpression = x => new { x.Id, x.Name });
```

throws

> Oops! For the options below, you need to use the `IncludeGraphOperationBuilder` (...) and specify the
> related entity type because those options depend on the current saved entity type (usually happens when
> using IncludeGraph, TPC, TPH, or TPT inheritance). Following options must be specified inside
> `IncludeGraphOperationBuilder`: ColumnInputExpression

although no graph or inheritance is involved, only an owned `ToJson()` navigation.

## Related issues

- zzzprojects/EntityFramework-Extensions#560: initial jsonb / `ToJson()` support; "Owned Many" was noted as not fully supported yet.
- zzzprojects/EntityFramework-Extensions#642: owned `ToJson()` with a primitive list on Npgsql (column-name bug, fixed).
- zzzprojects/EntityFramework-Extensions#568: `OwnsMany().ToJson()` with BulkUpdate/BulkMerge (still open).
