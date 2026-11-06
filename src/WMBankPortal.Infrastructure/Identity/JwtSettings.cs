namespace WMBankPortal.Infrastructure.Identity;

public sealed class JwtSettings
{
    public const string SectionName = "JwtSettings";

    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "WMBankPortal";
    public string Audience { get; set; } = "WMBankPortal";
    public int AccessTokenExpirationMinutes { get; set; } = 15;
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
