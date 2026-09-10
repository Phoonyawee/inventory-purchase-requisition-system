using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StockPilot.Data;
using StockPilot.Models;

namespace StockPilot.Pages;

public class IndexModel(ApplicationDbContext db) : PageModel
{
    public int ItemCount { get; private set; }
    public int LowStockCount { get; private set; }
    public int PendingRequests { get; private set; }
    public decimal InventoryValue { get; private set; }
    public IReadOnlyList<PurchaseRequest> RecentRequests { get; private set; } = [];

    public async Task OnGetAsync()
    {
        if (!(User.Identity?.IsAuthenticated ?? false)) return;
        ItemCount = await db.Items.CountAsync();
        LowStockCount = await db.Items.CountAsync(i => i.StockQuantity <= i.ReorderLevel);
        PendingRequests = await db.PurchaseRequests.CountAsync(r => r.Status == PurchaseRequestStatus.Pending);
        InventoryValue = await db.Items.SumAsync(i => i.StockQuantity * i.UnitCost);
        RecentRequests = await db.PurchaseRequests.AsNoTracking().Include(r => r.Item).OrderByDescending(r => r.CreatedAt).Take(5).ToListAsync();
    }
}
