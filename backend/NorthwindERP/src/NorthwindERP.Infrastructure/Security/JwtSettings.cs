namespace NorthwindERP.Infrastructure.Security;

// Se llena con la sección "Jwt" de appsettings.json (y la Key desde user-secrets)
public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 480;
}
