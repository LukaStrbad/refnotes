namespace Api.Exceptions;

public class UserIsOwnerException(string message) : Exception(message);
