using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ProductManagement.Application.Common.Exceptions;
using ProductManagement.Application.DTOs.Auth;
using ProductManagement.Application.Interfaces;
using ProductManagement.Domain.Entities;
using ProductManagement.Domain.Enums;
using ProductManagement.Domain.Interfaces;

namespace ProductManagement.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IUnitOfWork unitOfWork, IJwtService jwtService, ILogger<AuthService> _logger)
        {
            _unitOfWork = unitOfWork;
            _jwtService = jwtService;
            this._logger = _logger;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request, string ipAddress, CancellationToken cancellationToken = default)
        {
            var existingUserByEmail = await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);

            if (existingUserByEmail != null)
            {
                throw new AuthenticationException("Email is already registered.");
            }

            var existingUserByUsername = await _unitOfWork.Users.GetByUsernameAsync(request.Username, cancellationToken);

            if (existingUserByUsername != null)
            {
                throw new AuthenticationException("Username is already taken.");
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var usersExist = await _unitOfWork.Users.AnyAsync(cancellationToken);
            var role = !usersExist ? Role.Admin : Role.User;

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = request.Username,
                Email = request.Email,
                PasswordHash = passwordHash,
                Role = role,
                CreatedAt = DateTime.UtcNow
            };

            var refreshToken = _jwtService.GenerateRefreshToken(ipAddress);

            user.RefreshTokens.Add(refreshToken);

            await _unitOfWork.Users.AddAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var accessToken = _jwtService.GenerateAccessToken(user);

            _logger.LogInformation("User {Username} registered successfully with role {Role}.", user.Username, user.Role);

            return new AuthResponse(
                user.Id,
                user.Username,
                user.Email,
                user.Role.ToString(),
                accessToken,
                refreshToken.Token,
                refreshToken.Expires
            );
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request, string ipAddress, CancellationToken cancellationToken = default)
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Failed login attempt for email {Email}.", request.Email);
                throw new AuthenticationException("Invalid email or password.");
            }

            var refreshToken = _jwtService.GenerateRefreshToken(ipAddress);

            var inactiveTokens = user.RefreshTokens
                .Where(t => !t.IsActive && t.Expires <= DateTime.UtcNow.AddDays(-7))
                .ToList();

            foreach (var token in inactiveTokens)
            {
                user.RefreshTokens.Remove(token);
            }

            user.RefreshTokens.Add(refreshToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var accessToken = _jwtService.GenerateAccessToken(user);

            _logger.LogInformation("User {Email} logged in successfully.", user.Email);

            return new AuthResponse(
                user.Id,
                user.Username,
                user.Email,
                user.Role.ToString(),
                accessToken,
                refreshToken.Token,
                refreshToken.Expires
            );
        }

        public async Task<AuthResponse> RefreshTokenAsync(string token, string ipAddress, CancellationToken cancellationToken = default)
        {
            var user = await _unitOfWork.Users.GetByRefreshTokenAsync(token, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("Refresh token lookup failed: token not found.");
                throw new AuthenticationException("Invalid refresh token.");
            }

            var existingToken = user.RefreshTokens.Single(t => t.Token == token);

            if (!existingToken.IsActive)
            {
                _logger.LogWarning("Compromised refresh token reuse detected for user {UserId} from IP {IpAddress}! Revoking all active sessions.", user.Id, ipAddress);
                foreach (var activeToken in user.RefreshTokens.Where(t => t.IsActive))
                {
                    activeToken.RevokedAt = DateTime.UtcNow;
                    activeToken.RevokedByIp = ipAddress;
                }

                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                throw new AuthenticationException("Compromised refresh token reused. All active sessions revoked.");
            }

            var newRefreshToken = _jwtService.GenerateRefreshToken(ipAddress);

            existingToken.RevokedAt = DateTime.UtcNow;
            existingToken.RevokedByIp = ipAddress;
            existingToken.ReplacedByToken = newRefreshToken.Token;

            user.RefreshTokens.Add(newRefreshToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var accessToken = _jwtService.GenerateAccessToken(user);

            _logger.LogInformation("Token refreshed successfully for user {UserId}.", user.Id);

            return new AuthResponse(
                user.Id,
                user.Username,
                user.Email,
                user.Role.ToString(),
                accessToken,
                newRefreshToken.Token,
                newRefreshToken.Expires
            );
        }

        public async Task LogoutAsync(string token, CancellationToken cancellationToken = default)
        {
            var user = await _unitOfWork.Users.GetByRefreshTokenAsync(token, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("Logout failed: invalid token.");
                throw new AuthenticationException("Invalid token.");
            }

            var existingToken = user.RefreshTokens.Single(t => t.Token == token);

            if (existingToken.IsActive)
            {
                existingToken.RevokedAt = DateTime.UtcNow;

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("User {UserId} logged out successfully.", user.Id);
            }
        }
    }
}