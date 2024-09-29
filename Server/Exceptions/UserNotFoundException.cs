namespace Server.Exceptions;

public class UserNotFoundException(string message) : Exception(message);