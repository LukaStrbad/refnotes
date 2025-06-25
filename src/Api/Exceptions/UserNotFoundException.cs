namespace Api.Exceptions;

public class UserNotFoundException(string message) : Exception(message);