namespace Api.Tests.Data.Attributes;

public abstract class SutAttribute : Attribute
{
   public abstract Type SutType { get; } 
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class SutAttribute<T> : SutAttribute where T : class
{
   public override Type SutType { get; } = typeof(T);
}