namespace OnlineStore.Services;

public sealed class OrderTrackingNode
{
    public int StepIndex { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? FormattedTime { get; set; }

    public bool IsReached { get; set; }

    public bool IsCurrentActive { get; set; }

    public bool IsFutureEstimated { get; set; }
}

public sealed class OrderTrackingView
{
    public int OrderId { get; set; }

    public string OrderNo { get; set; } = string.Empty;

    public string Carrier { get; set; } = string.Empty;

    public string TrackingNumber { get; set; } = string.Empty;

    public string OfficialTrackingUrl { get; set; } = string.Empty;

    public string OriginLocation { get; set; } = string.Empty;

    public string DestinationCity { get; set; } = string.Empty;

    public string DestinationPostalCode { get; set; } = string.Empty;

    public string RecipientName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime? ShippedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public List<OrderTrackingNode> Nodes { get; set; } = new();
}
