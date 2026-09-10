using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StockPilot.Data;
using StockPilot.Models;
using StockPilot.Services;

namespace StockPilot.Pages;

[Authorize]
public class PurchaseRequestsModel(ApplicationDbContext db, UserManager<IdentityUser> users) : PageModel
{
    public IReadOnlyList<PurchaseRequest> Requests { get; private set; } = [];
    public IReadOnlyList<Item> Items { get; private set; } = [];
    public bool CanDecide => User.IsInRole("Manager") || User.IsInRole("Admin");
    public bool CanProcure => User.IsInRole("Inventory") || User.IsInRole("Admin");
    private bool CanViewAll => CanDecide || CanProcure;
    [BindProperty] public RequestInput Input { get; set; } = new();

    public sealed class RequestInput
    {
        [Range(1, int.MaxValue)] public int ItemId { get; set; }
        [Range(1, 10000)] public int Quantity { get; set; } = 1;
        [Required, StringLength(60)] public string Department { get; set; } = "";
        [Required, StringLength(500)] public string Reason { get; set; } = "";
    }

    public async Task OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (!await db.Items.AnyAsync(i => i.Id == Input.ItemId)) ModelState.AddModelError("Input.ItemId", "Select a valid item.");
        if (!ModelState.IsValid) { await LoadAsync(); return Page(); }
        var user = await users.GetUserAsync(User) ?? throw new InvalidOperationException("Signed-in user not found.");
        var request = new PurchaseRequest { ItemId = Input.ItemId, Quantity = Input.Quantity, Department = Input.Department.Trim(), Reason = Input.Reason.Trim(), RequestedById = user.Id, RequestedByName = user.Email ?? "Employee" };
        db.PurchaseRequests.Add(request);
        db.AuditLogs.Add(Log("Created purchase request", $"{Input.Quantity} units"));
        await db.SaveChangesAsync();
        TempData["Message"] = $"Purchase request #{request.Id} submitted.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostMoveAsync(int id, PurchaseRequestStatus next)
    {
        var request = await db.PurchaseRequests.Include(r => r.Item).SingleOrDefaultAsync(r => r.Id == id);
        if (request is null) return NotFound();
        var authorized = request.Status == PurchaseRequestStatus.Pending ? CanDecide : CanProcure;
        if (!authorized) return Forbid();
        if (!PurchaseWorkflow.CanMove(request.Status, next)) return BadRequest("Invalid purchase request transition.");

        request.Status = next;
        if (next is PurchaseRequestStatus.Approved or PurchaseRequestStatus.Rejected)
        {
            request.ReviewedAt = DateTime.UtcNow;
            request.ReviewedBy = User.Identity?.Name;
        }
        if (next == PurchaseRequestStatus.Received)
        {
            request.Item.StockQuantity += request.Quantity;
            db.StockMovements.Add(new StockMovement { ItemId = request.ItemId, Type = StockMovementType.Receipt, Quantity = request.Quantity, Note = $"Purchase request #{request.Id}", CreatedBy = User.Identity?.Name ?? "Unknown" });
        }
        db.AuditLogs.Add(Log($"Moved request to {next}", $"PR #{request.Id}"));
        await db.SaveChangesAsync();
        TempData["Message"] = $"Request #{request.Id} moved to {next}.";
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        Items = await db.Items.AsNoTracking().OrderBy(i => i.Sku).ToListAsync();
        var query = db.PurchaseRequests.AsNoTracking().Include(r => r.Item).AsQueryable();
        if (!CanViewAll)
        {
            var userId = users.GetUserId(User);
            query = query.Where(r => r.RequestedById == userId);
        }
        Requests = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
    }

    private AuditLog Log(string action, string record) => new() { Actor = User.Identity?.Name ?? "Unknown", Action = action, Record = record };
}
