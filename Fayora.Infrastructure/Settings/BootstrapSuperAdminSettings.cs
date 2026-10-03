namespace Fayora.Infrastructure.Settings;

public sealed class BootstrapSuperAdminSettings
{
    public const string SectionName = "BootstrapSuperAdmin";

    public bool Enabled { get; init; } = true;
    public string FirstName { get; init; } = "Fayora";
    public string LastName { get; init; } = "Admin";
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
