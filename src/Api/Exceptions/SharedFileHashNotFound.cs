namespace Api.Exceptions;

public sealed class SharedFileHashNotFound(string message) : Exception(message);
