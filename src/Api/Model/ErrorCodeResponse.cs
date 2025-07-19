namespace Api.Model;

/// <summary>
/// Provides predefined error codes for API responses
/// </summary>
public static class ErrorCodes
{
    public static readonly ErrorCodeResponse EmailNotConfirmed = new("EMAIL_NOT_CONFIRMED");
    public static readonly ErrorCodeResponse InvalidOrExpiredToken = new("INVALID_OR_EXPIRED_TOKEN");
    public static readonly ErrorCodeResponse UserNotFound = new("USER_NOT_FOUND");
}

/// <summary>
/// Represents an error code response
/// </summary>
/// <param name="ErrorCode">The error code string</param>
// ReSharper disable once NotAccessedPositionalProperty.Global
public sealed record ErrorCodeResponse(string ErrorCode);
