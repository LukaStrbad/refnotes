namespace ServerTests.Data.Attributes;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class RandomStringAttribute : Attribute
{
    public string Prefix { get; }
    public int Length { get; init; } = 32;
    
    public RandomStringAttribute(string prefix = "")
    {
        Prefix = prefix;
    }
}