namespace CatalogApi.Models;

public class Product
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool IsOutOfStock => Stock <= 0;
    public string? AvailabilityBadgeText => IsOutOfStock ? "Out of Stock" : null;
    public string? AvailabilityBadgeClass => IsOutOfStock ? "badge bg-secondary" : null;
}
