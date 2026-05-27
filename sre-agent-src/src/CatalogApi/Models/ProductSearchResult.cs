namespace CatalogApi.Models;

public class ProductSearchResult
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int Stock { get; init; }
    public bool IsOutOfStock { get; init; }
    public string? AvailabilityBadgeText { get; init; }
    public string? AvailabilityBadgeClass { get; init; }

    public static ProductSearchResult FromProduct(Product product)
    {
        var isOutOfStock = product.Stock <= 0;

        return new ProductSearchResult
        {
            Id = product.Id,
            Name = product.Name,
            Category = product.Category,
            Description = product.Description,
            Price = product.Price,
            Stock = product.Stock,
            IsOutOfStock = isOutOfStock,
            AvailabilityBadgeText = isOutOfStock ? "Out of Stock" : null,
            AvailabilityBadgeClass = isOutOfStock ? "badge bg-secondary" : null
        };
    }
}
