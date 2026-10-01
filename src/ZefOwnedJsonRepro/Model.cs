namespace ZefOwnedJsonRepro;

public class Team
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public List<TeamMember> Members { get; set; } = new();
}

// Owned type stored as a single jsonb document via ToJson().
public class TeamMember
{
    public Guid? PersonId { get; set; }
    public decimal Share { get; set; }
    public MemberRole Role { get; set; }
    public DateTimeOffset JoinedAt { get; set; }

    // Primitive collection inside the JSON document - the only member ZEF writes incorrectly.
    public List<Guid> TagIds { get; set; } = new();
}

public enum MemberRole
{
    Lead,
    Contributor
}
