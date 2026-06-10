using System.Collections.Generic;
using System.Security.Claims;
using ProductManagement.Domain.Entities;

namespace ProductManagement.Application.Interfaces
{
    public interface IJwtService
    {
        string GenerateAccessToken(User user);
        RefreshToken GenerateRefreshToken(string ipAddress);
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    }
}
