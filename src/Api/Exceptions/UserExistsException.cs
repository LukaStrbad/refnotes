namespace Api.Exceptions;

public class UserExistsException(string message) : Exception(message);
