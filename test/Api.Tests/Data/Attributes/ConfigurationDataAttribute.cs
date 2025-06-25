namespace Api.Tests.Data.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class ConfigurationDataAttribute : Attribute
{
    public string FunctionName { get; }

    public ConfigurationDataAttribute(string functionName)
    {
        FunctionName = functionName;
    }
}