namespace Server.Exceptions;

public class ExpiryTimeTooLongException(string message) : Exception(message);