namespace Api.Tests.Data.Attributes;

public abstract class ConcreteTypeAttribute : Attribute
{
    public abstract Type? DeclaringType { get; }
    public abstract Type ImplementationType { get; }
}

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class ConcreteTypeAttribute<T> : ConcreteTypeAttribute where T : class
{
    public override Type? DeclaringType => null;
    public override Type ImplementationType { get; } = typeof(T);
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class ConcreteTypeAttribute<T, TImpl> : ConcreteTypeAttribute where T : class where TImpl : class
{
    public override Type? DeclaringType { get; } = typeof(T);
    public override Type ImplementationType { get; } = typeof(TImpl);
}
