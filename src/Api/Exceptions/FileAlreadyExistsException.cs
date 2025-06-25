namespace Api.Exceptions;

public class FileAlreadyExistsException(string message) : Exception(message);