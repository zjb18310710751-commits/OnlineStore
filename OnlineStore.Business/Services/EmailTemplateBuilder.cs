namespace OnlineStore.Services;

public static class EmailTemplateBuilder
{
    public const string VerificationCodeSubject = "Your ZHY verification code";

    public static string BuildVerificationCode(string code, int expiryMinutes = 10)
    {
        return $"""
        <div style="max-width:480px;margin:0 auto;padding:24px;font-family:Arial,'Helvetica Neue',sans-serif;color:#333;">
            <div style="text-align:center;padding:8px 0 16px;">
                <span style="font-size:20px;font-weight:bold;color:#111;">ZHY Store</span>
            </div>
            <div style="background:#f7f8fa;border-radius:8px;padding:24px;">
                <p style="margin:0 0 12px;font-size:15px;">You requested an email verification code:</p>
                <div style="text-align:center;margin:20px 0;">
                    <span style="display:inline-block;font-size:32px;font-weight:bold;letter-spacing:8px;color:#d6336c;background:#fff;border:1px dashed #d6336c;border-radius:8px;padding:12px 24px;">{code}</span>
                </div>
                <p style="margin:12px 0 0;font-size:13px;color:#666;">This code expires in {expiryMinutes} minutes. Do not share it with anyone.</p>
            </div>
            <p style="margin:16px 0 0;font-size:12px;color:#999;text-align:center;">If you did not request this email, you can safely ignore it.</p>
        </div>
        """;
    }
}
