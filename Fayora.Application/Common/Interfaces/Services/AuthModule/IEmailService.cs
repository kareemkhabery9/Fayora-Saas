namespace Fayora.Application.Common.Interfaces.Services.AuthModule;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
    Task SendEmailWithInlineImageAsync(string to, string subject, string body, byte[] imageBytes, string contentId, string imageFileName);
}
