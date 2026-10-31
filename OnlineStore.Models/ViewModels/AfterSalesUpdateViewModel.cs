using OnlineStore.Models;

namespace OnlineStore.ViewModels;

public sealed record AfterSalesUpdateViewModel(int Id, string Status, string StateKey)
{
    public static AfterSalesUpdateViewModel FromRequest(AfterSalesRequest request) =>
        new(
            request.Id,
            request.Status,
            $"{request.Status}|{request.ReturnCarrier}|{request.ReturnTrackingNumber}|{request.ReturnShippedAt:O}|{request.ReceivedAt:O}|{request.CompletedAt:O}|{request.AdminNote}");
}
