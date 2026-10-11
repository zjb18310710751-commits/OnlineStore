namespace OnlineStore.Services;

/// <summary>邮件发送抽象。当前用 SMTP 实现，后续可替换为第三方邮件服务（可修改性）。</summary>
public interface IEmailService
{
    /// <summary>发送一封 HTML 邮件。</summary>
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
