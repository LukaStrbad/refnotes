namespace Server.Exceptions;

public class DirectoryNotEmptyException(string message) : Exception(message);