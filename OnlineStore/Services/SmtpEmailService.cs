using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace OnlineStore.Services;

/// <summary>基于 SMTP 的邮件发送实现（MailKit），配置从 Email 段读取。</summary>
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
            throw new InvalidOperationException("邮件服务未配置，请联系管理员。");
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
            // 465 端口是隐式 SSL（SMTPS）；其余端口按配置走 STARTTLS
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
            _logger.LogError(ex, "发送验证码邮件失败：{To}", to);
            throw new InvalidOperationException("验证码发送失败，请稍后再试。", ex);
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
