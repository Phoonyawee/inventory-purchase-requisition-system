using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StockPilot.Data;
using StockPilot.Models;

namespace StockPilot.Pages;

[Authorize]
public class InventoryModel(ApplicationDbContext db) : PageModel
{
    public IReadOnlyList<Item> Items { get; private set; } = [];
    public IReadOnlyList<StockMovement> Movements { get; private set; } = [];
    public bool CanManage => User.IsInRole("Inventory") || User.IsInRole("Admin");
    [BindProperty] public ItemInput NewItem { get; set; } = new();
    [BindProperty] public MovementInput Movement { get; set; } = new();

    public sealed class ItemInput
    {
        [Required, StringLength(30)] public string Sku { get; set; } = "";
        [Required, StringLength(100)] public string Name { get; set; } = "";
        [Required, StringLength(60)] public string Category { get; set; } = "";
        [Required, StringLength(20)] public string Unit { get; set; } = "pcs";
        [Range(0, int.MaxValue)] public int ReorderLevel { get; set; }
        [Range(0, double.MaxValue)] public decimal UnitCost { get; set; }
    }

    public sealed class MovementInput
    {
        [Range(1, int.MaxValue)] public int ItemId { get; set; }
        public StockMovementType Type { get; set; }
        [Range(1, 100000)] public int Quantity { get; set; } = 1;
        [Required, StringLength(200)] public string Note { get; set; } = "";
    }

    public async Task OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnPostAddItemAsync()
    {
        if (!CanManage) return Forbid();
        var sku = NewItem.Sku.Trim().ToUpperInvariant();
        if (await db.Items.AnyAsync(i => i.Sku == sku)) ModelState.AddModelError("NewItem.Sku", "SKU already exists.");
        if (!ModelState.IsValid) { await LoadAsync(); return Page(); }
        var item = new Item { Sku = sku, Name = NewItem.Name.Trim(), Category = NewItem.Category.Trim(), Unit = NewItem.Unit.Trim(), ReorderLevel = NewItem.ReorderLevel, UnitCost = NewItem.UnitCost };
        db.Items.Add(item);
        db.AuditLogs.Add(Log("Created item", item.Sku));
        await db.SaveChangesAsync();
        TempData["Message"] = $"Item {item.Sku} created.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostMoveAsync()
    {
        if (!CanManage) return Forbid();
        var item = await db.Items.FindAsync(Movement.ItemId);
        if (item is null) return NotFound();
        if (Movement.Type == StockMovementType.Issue && Movement.Quantity > item.StockQuantity)
            ModelState.AddModelError("Movement.Quantity", "Issue quantity exceeds available stock.");
        if (!ModelState.IsValid) { await LoadAsync(); return Page(); }

        item.StockQuantity += Movement.Type == StockMovementType.Receipt ? Movement.Quantity : -Movement.Quantity;
        db.StockMovements.Add(new StockMovement { ItemId = item.Id, Type = Movement.Type, Quantity = Movement.Quantity, Note = Movement.Note.Trim(), CreatedBy = User.Identity?.Name ?? "Unknown" });
        db.AuditLogs.Add(Log($"Recorded {Movement.Type.ToString().ToLowerInvariant()}", $"{item.Sku} × {Movement.Quantity}"));
        await db.SaveChangesAsync();
        TempData["Message"] = $"Stock updated for {item.Sku}.";
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        Items = await db.Items.AsNoTracking().OrderBy(i => i.Sku).ToListAsync();
        Movements = await db.StockMovements.AsNoTracking().Include(m => m.Item).OrderByDescending(m => m.CreatedAt).Take(10).ToListAsync();
    }

    private AuditLog Log(string action, string record) => new() { Actor = User.Identity?.Name ?? "Unknown", Action = action, Record = record };
}
