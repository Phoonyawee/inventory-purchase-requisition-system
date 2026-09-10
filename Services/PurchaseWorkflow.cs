using StockPilot.Models;

namespace StockPilot.Services;

public static class PurchaseWorkflow
{
    public static bool CanMove(PurchaseRequestStatus current, PurchaseRequestStatus next) =>
        (current, next) is
            (PurchaseRequestStatus.Pending, PurchaseRequestStatus.Approved) or
            (PurchaseRequestStatus.Pending, PurchaseRequestStatus.Rejected) or
            (PurchaseRequestStatus.Approved, PurchaseRequestStatus.Ordered) or
            (PurchaseRequestStatus.Ordered, PurchaseRequestStatus.Received);

    public static void SelfCheck()
    {
        if (!CanMove(PurchaseRequestStatus.Pending, PurchaseRequestStatus.Approved) ||
            !CanMove(PurchaseRequestStatus.Ordered, PurchaseRequestStatus.Received) ||
            CanMove(PurchaseRequestStatus.Rejected, PurchaseRequestStatus.Received))
            throw new InvalidOperationException("Purchase workflow self-check failed.");
        Console.WriteLine("Purchase workflow self-check passed.");
    }
}
