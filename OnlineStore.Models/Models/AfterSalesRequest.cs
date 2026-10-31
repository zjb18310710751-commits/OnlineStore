using System.ComponentModel.DataAnnotations;

namespace OnlineStore.Models;

public class AfterSalesRequest
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public Order? Order { get; set; }

    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    [StringLength(20)]
    public string Type { get; set; } = AfterSalesType.Return;

    [StringLength(1000)]
    public string Reason { get; set; } = string.Empty;

    [StringLength(20)]
    public string Status { get; set; } = AfterSalesStatus.Pending;

    [StringLength(500)]
    public string? AdminNote { get; set; }

    [StringLength(50)]
    public string? ReturnCarrier { get; set; }

    [StringLength(50)]
    public string? ReturnTrackingNumber { get; set; }

    public DateTime? ReturnShippedAt { get; set; }

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReviewedAt { get; set; }

    public DateTime? ReceivedAt { get; set; }

    public DateTime? CompletedAt { get; set; }
}
