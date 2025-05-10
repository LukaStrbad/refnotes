namespace Server.Exceptions;

public class UserIsOwnerException(string message) : Exception(message);