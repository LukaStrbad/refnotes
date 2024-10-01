namespace Server.Exceptions;

public class RefreshTokenInvalid(string message) : Exception(message);