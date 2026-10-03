using Fayora.Application.Common.Interfaces.Services.AuthModule;
using Fayora.Infrastructure.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;

namespace Fayora.Infrastructure.Services.AuthModule;

public class EmailService(IOptions<EmailSettings> emailSettingsOptions, ILogger<EmailService> logger) : IEmailService
{
    private readonly EmailSettings _emailSettings = emailSettingsOptions.Value;

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        await SendAsync(to, subject, new TextPart(TextFormat.Html) { Text = body });
    }

    public async Task SendEmailWithInlineImageAsync(string to, string subject, string body, byte[] imageBytes, string contentId, string imageFileName)
    {
        var image = new MimePart("image", "png")
        {
            Content = new MimeContent(new MemoryStream(imageBytes)),
            ContentId = contentId,
            ContentDisposition = new ContentDisposition(ContentDisposition.Inline),
            FileName = imageFileName
        };
        var multipart = new Multipart("related")
        {
            new TextPart(TextFormat.Html) { Text = body },
            image
        };
        await SendAsync(to, subject, multipart);
    }

    private async Task SendAsync(string to, string subject, MimeEntity body)
    {
        try
        {
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(_emailSettings.Sender));
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = subject;

            email.Body = body;

            using var smtp = new SmtpClient();

            var socketOptions = _emailSettings.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
            await smtp.ConnectAsync(_emailSettings.Host, _emailSettings.Port, socketOptions);

            await smtp.AuthenticateAsync(_emailSettings.Sender, _emailSettings.Password);

            await smtp.SendAsync(email);

            await smtp.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while sending email to {Receiver}.", to);

            throw new InvalidOperationException("Failed to send email.", ex);
        }
    }
}
