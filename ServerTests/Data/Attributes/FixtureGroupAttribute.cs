namespace ServerTests.Data.Attributes;

public sealed class FixtureGroupAttribute : Attribute
{
    public string? ForUser { get; init; }
    public string? GroupName { get; init; }
    public bool AddNull { get; init; } = false;
}