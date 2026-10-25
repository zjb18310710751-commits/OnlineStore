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
        var username = section["Username"];
        var password = section["Password"];
        var senderEmail = section["SenderEmail"];
        if (string.IsNullOrWhiteSpace(senderEmail))
        {
            senderEmail = username;
        }

        if (string.IsNullOrWhiteSpace(host))
        {
            throw new InvalidOperationException("邮件服务未配置 SMTP 主机。");
        }

        if (string.IsNullOrWhiteSpace(senderEmail))
        {
            throw new InvalidOperationException("邮件服务未配置发件邮箱。");
        }

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("邮件服务未配置 SMTP 登录邮箱或授权码。");
        }

        var port = section.GetValue("Port", 587);
        var enableSsl = section.GetValue("EnableSsl", true);
        var senderName = section["SenderName"] ?? "Online Store";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(senderName, senderEmail));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient
        {
            Timeout = 20_000
        };
        try
        {

            var secureOptions = port == 465
                ? SecureSocketOptions.SslOnConnect
                : (enableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

            await client.ConnectAsync(host, port, secureOptions, cancellationToken);

            await client.AuthenticateAsync(username, password, cancellationToken);

            await client.SendAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送验证码邮件失败：{To}", to);
            throw new InvalidOperationException($"验证码发送失败（{host}:{port}）：{ex.Message}", ex);
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
