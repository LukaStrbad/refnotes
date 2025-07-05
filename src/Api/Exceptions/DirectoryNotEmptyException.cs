namespace Api.Exceptions;

public class DirectoryNotEmptyException(string message) : Exception(message);
