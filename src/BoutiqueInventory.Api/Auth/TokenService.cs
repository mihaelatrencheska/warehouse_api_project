using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BoutiqueInventory.Application.DTOs.Requests;
using BoutiqueInventory.Application.DTOs.Responses;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BoutiqueInventory.Api.Auth;

/// <summary>Issues and validates JWT access tokens and single-use refresh tokens.</summary>
public sealed class TokenService(IOptions<JwtOptions> jwtOptions, IOptions<BoutiqueAuthOptions> authOptions)
{
    private readonly ConcurrentDictionary<string, RefreshTokenEntry> _refreshTokens = new();

    /// <summary>Validates credentials and returns a new token pair, or <c>null</c> on failure.</summary>
    public LoginResponse? TryLogin(LoginRequest request)
    {
        var auth = authOptions.Value;
        if (!string.Equals(request.Username, auth.Username, StringComparison.Ordinal) ||
            !string.Equals(request.Password, auth.Password, StringComparison.Ordinal))
        {
            return null;
        }

        return IssueTokenPair(auth.Username);
    }

    /// <summary>Consumes a refresh token and returns a new token pair, or <c>null</c> if invalid or expired.</summary>
    public LoginResponse? TryRefresh(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken) ||
            !_refreshTokens.TryRemove(refreshToken, out var entry) ||
            entry.ExpiresAt <= DateTime.UtcNow)
        {
            return null;
        }

        return IssueTokenPair(entry.Username);
    }

    /// <summary>Generates a cryptographically random 64-byte base-64 refresh token string.</summary>
    public string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private LoginResponse IssueTokenPair(string username)
    {
        var jwt = jwtOptions.Value;
        var accessToken = CreateAccessToken(username, jwt);
        var refreshToken = GenerateRefreshToken();
        var refreshLifetime = TimeSpan.FromDays(jwt.RefreshExpirationDays);

        _refreshTokens[refreshToken] = new RefreshTokenEntry(username, DateTime.UtcNow.Add(refreshLifetime));

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresInSeconds = (int)TimeSpan.FromMinutes(jwt.ExpirationMinutes).TotalSeconds
        };
    }

    private static string CreateAccessToken(string username, JwtOptions jwt)
    {
        var expires = DateTime.UtcNow.AddMinutes(jwt.ExpirationMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwt.Issuer,
            audience: jwt.Audience,
            claims:
            [
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, "Owner")
            ],
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed record RefreshTokenEntry(string Username, DateTime ExpiresAt);
}
