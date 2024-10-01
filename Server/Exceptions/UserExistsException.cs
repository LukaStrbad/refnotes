namespace Server.Exceptions;

public class UserExistsException(string message) : Exception(message);