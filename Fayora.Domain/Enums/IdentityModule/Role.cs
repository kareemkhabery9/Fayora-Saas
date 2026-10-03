namespace Fayora.Domain.Enums.IdentityModule;

[Flags]
public enum Role
{
    // Values are kept stable so existing company and super-admin accounts remain authorized.
    Company = 128,
    SuperAdmin = 256,
}
