using System.Globalization;
using System.Net;

namespace OnlineStore.Infrastructure;

public static class FailureResponse
{
    public static async Task WriteAsync(HttpContext context, int status, string code, int retrySeconds)
    {
        context.Response.StatusCode = status;
        context.Response.Headers.RetryAfter = retrySeconds.ToString(CultureInfo.InvariantCulture);
        context.Response.Headers.CacheControl = "no-store";
        var chinese = CultureInfo.CurrentUICulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase);
        var limited = status == StatusCodes.Status429TooManyRequests;
        var title = limited
            ? (chinese ? "操作有些频繁，请稍后再试" : "Too many requests. Please try again shortly.")
            : (chinese ? "服务暂时不可用" : "Service temporarily unavailable");
        var message = limited
            ? (chinese ? $"请等待 {retrySeconds} 秒后再试。" : $"Please wait {retrySeconds} seconds before trying again.")
            : (chinese ? "我们暂时无法连接数据服务。请稍后查看订单状态，再决定是否重新提交。" : "We cannot reach the data service. Check your order status after recovery before submitting again.");
        if (context.Request.HasJsonContentType()
            || context.Request.Headers.Accept.Any(value => value?.Contains("json", StringComparison.OrdinalIgnoreCase) == true)
            || context.Request.Headers.XRequestedWith == "XMLHttpRequest"
            || context.Request.Path.Equals("/Account/SendCode", StringComparison.OrdinalIgnoreCase))
        {
            await context.Response.WriteAsJsonAsync(new { ok = false, code, error = title, message, retryAfterSeconds = retrySeconds, requestId = context.TraceIdentifier });
            return;
        }
        context.Response.ContentType = "text/html; charset=utf-8";
        var language = chinese ? "zh-CN" : "en";
        var home = chinese ? "返回首页" : "Back to home";
        await context.Response.WriteAsync($$"""
            <!doctype html><html lang="{{language}}"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>{{title}} · ZHY</title>
            <style>body{margin:0;background:#fff;color:#111;font-family:system-ui,sans-serif}header{padding:24px 5vw;border-bottom:1px solid #eee;font-size:28px;font-weight:900}main{max-width:650px;margin:12vh auto;padding:0 24px;text-align:center}h1{font-size:clamp(26px,5vw,40px)}p{line-height:1.7}.code{font-size:64px;font-weight:800}a{display:inline-block;background:#111;color:#fff;padding:14px 26px;border-radius:30px;text-decoration:none}a:focus-visible{outline:3px solid #1473e6;outline-offset:4px}small{display:block;margin-top:32px;overflow-wrap:anywhere;color:#666}</style></head>
            <body><header>ZHY.</header><main><div class="code">{{status}}</div><h1>{{title}}</h1><p>{{message}}</p><a href="/">{{home}}</a><small>Request ID: {{WebUtility.HtmlEncode(context.TraceIdentifier)}}</small></main></body></html>
            """);
    }
}
