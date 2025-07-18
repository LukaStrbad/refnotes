namespace Api.Model;

public record UpdatePasswordRequest(string OldPassword, string NewPassword);

public record UpdatePasswordByTokenRequest(string Username, string Password, string Token);
