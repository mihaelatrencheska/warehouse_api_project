namespace BoutiqueInventory.Api.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Auth:Jwt";

    public string Issuer { get; set; } = "boutique-inventory";
    public string Audience { get; set; } = "boutique-clients";
    public string SigningKey { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 480;
    public int RefreshExpirationDays { get; set; } = 7;
}

public sealed class BoutiqueAuthOptions
{
    public const string SectionName = "Auth";

    public string Username { get; set; } = "owner";
    public string Password { get; set; } = string.Empty;
}
