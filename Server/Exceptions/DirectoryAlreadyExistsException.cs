namespace Server.Exceptions;

public class DirectoryAlreadyExistsException(string message): Exception(message);