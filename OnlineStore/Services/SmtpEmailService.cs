using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace OnlineStore.Services;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var section = _configuration.GetSection("Email");
        var host = section["Host"];
        var senderEmail = section["SenderEmail"];

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(senderEmail))
        {
            throw new InvalidOperationException("Email service is not configured.");
        }

        var port = section.GetValue("Port", 587);
        var enableSsl = section.GetValue("EnableSsl", true);
        var senderName = section["SenderName"] ?? "Online Store";
        var username = section["Username"];
        var password = section["Password"];

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(senderName, senderEmail));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient();
        try
        {

            var secureOptions = port == 465
                ? SecureSocketOptions.SslOnConnect
                : (enableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

            await client.ConnectAsync(host, port, secureOptions, cancellationToken);

            if (!string.IsNullOrWhiteSpace(username))
            {
                await client.AuthenticateAsync(username, password ?? string.Empty, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification email to {To}.", to);
            throw new InvalidOperationException(
                $"Verification email could not be sent through {host}:{port}.", ex);
        }
        finally
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync(true, cancellationToken);
            }
        }
    }
}
