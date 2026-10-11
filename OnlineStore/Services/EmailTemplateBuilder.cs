namespace OnlineStore.Services;

/// <summary>邮件 HTML 模板构建。</summary>
public static class EmailTemplateBuilder
{
    /// <summary>构建验证码邮件（内联样式，兼容邮件客户端）。</summary>
    public static string BuildVerificationCode(string code, int expiryMinutes = 10)
    {
        return $"""
        <div style="max-width:480px;margin:0 auto;padding:24px;font-family:'Microsoft YaHei',Arial,sans-serif;color:#333;">
            <div style="text-align:center;padding:8px 0 16px;">
                <span style="font-size:20px;font-weight:bold;color:#111;">在线商店</span>
            </div>
            <div style="background:#f7f8fa;border-radius:8px;padding:24px;">
                <p style="margin:0 0 12px;font-size:15px;">您正在申请邮箱验证，验证码为：</p>
                <div style="text-align:center;margin:20px 0;">
                    <span style="display:inline-block;font-size:32px;font-weight:bold;letter-spacing:8px;color:#d6336c;background:#fff;border:1px dashed #d6336c;border-radius:8px;padding:12px 24px;">{code}</span>
                </div>
                <p style="margin:12px 0 0;font-size:13px;color:#666;">验证码 {expiryMinutes} 分钟内有效，请勿泄露给他人。</p>
            </div>
            <p style="margin:16px 0 0;font-size:12px;color:#999;text-align:center;">如果这不是您本人的操作，请忽略本邮件。</p>
        </div>
        """;
    }
}
