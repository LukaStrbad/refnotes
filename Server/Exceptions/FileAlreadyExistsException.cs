namespace Server.Exceptions;

public class FileAlreadyExistsException(string message) : Exception(message);