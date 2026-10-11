using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace DomoNow.Parking.Api.IntegrationTests;

/// <summary>Fabrica JWT a medida para probar los caminos 401/403 que el login real no puede producir.</summary>
internal static class TestTokens
{
    public const string SigningKey = "integration-tests-signing-key-with-at-least-32-bytes!";

    public static string Create(
        string? role = "Operator",
        string signingKey = SigningKey,
        DateTime? expiresAtUtc = null)
    {
        var expires = expiresAtUtc ?? DateTime.UtcNow.AddMinutes(10);
        var claims = new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = Guid.NewGuid().ToString(),
            [JwtRegisteredClaimNames.UniqueName] = "tester",
        };
        if (role is not null)
        {
            claims["role"] = role;
        }

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "domonow-parking-api",
            Audience = "domonow-parking-web",
            Claims = claims,
            IssuedAt = expires.AddMinutes(-20),
            NotBefore = expires.AddMinutes(-20),
            Expires = expires,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)), SecurityAlgorithms.HmacSha256),
        });
    }
}
