using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StockPilot.Data;
using StockPilot.Models;

namespace StockPilot.Pages;

[Authorize(Roles = "Manager,Inventory,Admin")]
public class AuditModel(ApplicationDbContext db) : PageModel
{
    public IReadOnlyList<AuditLog> Logs { get; private set; } = [];
    public async Task OnGetAsync() => Logs = await db.AuditLogs.AsNoTracking().OrderByDescending(l => l.CreatedAt).Take(100).ToListAsync();
}
