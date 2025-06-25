namespace Api.Exceptions;

public class ExpiryTimeTooLongException(string message) : Exception(message);