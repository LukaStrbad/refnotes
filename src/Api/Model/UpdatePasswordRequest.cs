namespace Api.Model;

public record UpdatePasswordRequest(string OldPassword, string NewPassword);
