using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace OnlineStore.Services;

/// <summary>基于 SMTP 的邮件发送实现，配置从 Email 段读取。</summary>
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

        using var message = new MailMessage
        {
            From = new MailAddress(senderEmail, senderName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(to);

        using var smtp = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        if (!string.IsNullOrWhiteSpace(username))
        {
            smtp.Credentials = new NetworkCredential(username, password);
        }

        try
        {
            await smtp.SendMailAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送验证码邮件失败：{To}", to);
            throw new InvalidOperationException("验证码发送失败，请稍后再试。", ex);
        }
    }
}
