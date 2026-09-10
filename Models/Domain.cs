using System.ComponentModel.DataAnnotations;

namespace StockPilot.Models;

public enum StockMovementType { Receipt, Issue }
public enum PurchaseRequestStatus { Pending, Approved, Rejected, Ordered, Received }

public sealed class Item
{
    public int Id { get; set; }
    [Required, StringLength(30)] public string Sku { get; set; } = "";
    [Required, StringLength(100)] public string Name { get; set; } = "";
    [Required, StringLength(60)] public string Category { get; set; } = "";
    [Required, StringLength(20)] public string Unit { get; set; } = "pcs";
    [Range(0, int.MaxValue)] public int StockQuantity { get; set; }
    [Range(0, int.MaxValue)] public int ReorderLevel { get; set; }
    [Range(0, double.MaxValue)] public decimal UnitCost { get; set; }
}

public sealed class StockMovement
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public Item Item { get; set; } = null!;
    public StockMovementType Type { get; set; }
    public int Quantity { get; set; }
    [Required, StringLength(200)] public string Note { get; set; } = "";
    [Required, StringLength(100)] public string CreatedBy { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class PurchaseRequest
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public Item Item { get; set; } = null!;
    [Range(1, 10000)] public int Quantity { get; set; }
    [Required, StringLength(60)] public string Department { get; set; } = "";
    [Required, StringLength(500)] public string Reason { get; set; } = "";
    [Required] public string RequestedById { get; set; } = "";
    [Required, StringLength(100)] public string RequestedByName { get; set; } = "";
    public PurchaseRequestStatus Status { get; set; } = PurchaseRequestStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    [StringLength(100)] public string? ReviewedBy { get; set; }
}

public sealed class AuditLog
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string Actor { get; set; } = "";
    [Required, StringLength(100)] public string Action { get; set; } = "";
    [Required, StringLength(100)] public string Record { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
