using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace OnlineStore.Services;

public class BrevoApiEmailService : IEmailService
{
    private const string ApiEndpoint = "https://api.brevo.com/v3/smtp/email";

    private readonly IConfiguration _configuration;
    private readonly ILogger<BrevoApiEmailService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public BrevoApiEmailService(
        IConfiguration configuration,
        ILogger<BrevoApiEmailService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var section = _configuration.GetSection("Email");
        var apiKey = section["ApiKey"];
        var senderEmail = section["SenderEmail"];

        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(senderEmail))
        {
            throw new InvalidOperationException("邮件服务未配置，请联系管理员。");
        }

        var senderName = section["SenderName"] ?? "Online Store";

        var payload = new
        {
            sender = new { name = senderName, email = senderEmail },
            to = new[] { new { email = to } },
            subject,
            htmlContent = htmlBody
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, ApiEndpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("api-key", apiKey);
        request.Headers.Add("accept", "application/json");

        try
        {
            var client = _httpClientFactory.CreateClient();
            using var response = await client.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Brevo 发送失败（{Status}）：{Body}", response.StatusCode, body);
                throw new InvalidOperationException($"验证码发送失败（{response.StatusCode}）。");
            }
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送验证码邮件失败：{To}", to);
            throw new InvalidOperationException($"验证码发送失败：{ex.Message}", ex);
        }
    }
}
