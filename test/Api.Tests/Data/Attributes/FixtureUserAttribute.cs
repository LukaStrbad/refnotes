namespace Api.Tests.Data.Attributes;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class FixtureUserAttribute : Attribute
{
    public string? Username { get; init; }
    public string[]? Roles { get; init; }
}