namespace DevMentor.Application.Features.Auth.Dtos;

public record RegisterRequest(string FullName, string Email, string Password);
public record RegisterResult(Guid UserId, string Email, string Message);

public record LoginRequest(string Email, string Password);
public record RefreshRequest(string AccessToken, string RefreshToken);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Email, string Token, string NewPassword);
public record ConfirmEmailRequest(string Email, string Token);
public record ResendConfirmationRequest(string Email);

public record AuthResponse(
    Guid UserId,
    string FullName,
    string Email,
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAtUtc);