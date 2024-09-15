namespace Server.Model;

public record RefreshToken(string Token, DateTime ExpiryTime);

public record Tokens(string AccessToken, RefreshToken RefreshToken);