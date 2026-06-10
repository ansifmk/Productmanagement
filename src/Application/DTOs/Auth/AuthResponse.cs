namespace ProductManagement.Application.DTOs.Auth
{
    public record AuthResponse(Guid UserId, string Username, string Email, string Role, string AccessToken, string RefreshToken, DateTime ExpiresAt);
}
