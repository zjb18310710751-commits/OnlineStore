namespace OnlineStore.ViewModels;

/// <summary>错误页视图模型。</summary>
public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public int? StatusCode { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
