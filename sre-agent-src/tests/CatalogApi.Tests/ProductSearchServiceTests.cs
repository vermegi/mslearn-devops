using CatalogApi.Models;
using CatalogApi.Services;
using Xunit;

namespace CatalogApi.Tests;

public class ProductSearchServiceTests
{
    private readonly ProductSearchService _searchService = new();

    [Fact]
    public void ApplySearch_SortsInStockProductsBeforeOutOfStock()
    {
        var products = new[]
        {
            new Product { Name = "Gamma Cable", Category = "Accessories", Description = "In stock match", Stock = 10 },
            new Product { Name = "Alpha Cable", Category = "Accessories", Description = "Out of stock match", Stock = 0 },
            new Product { Name = "Beta Cable", Category = "Accessories", Description = "In stock match", Stock = 5 }
        };

        var results = _searchService.ApplySearch(products, "cable", hideOutOfStock: false).ToList();

        Assert.Collection(results,
            product => Assert.Equal("Beta Cable", product.Name),
            product => Assert.Equal("Gamma Cable", product.Name),
            product => Assert.Equal("Alpha Cable", product.Name));
    }

    [Fact]
    public void ApplySearch_HidesOutOfStockProductsWhenRequested()
    {
        var products = new[]
        {
            new Product { Name = "Keyboard", Category = "Electronics", Description = "Available", Stock = 8 },
            new Product { Name = "Keyboard Sleeve", Category = "Accessories", Description = "Unavailable", Stock = 0 }
        };

        var results = _searchService.ApplySearch(products, "keyboard", hideOutOfStock: true).ToList();

        Assert.Single(results);
        Assert.Equal("Keyboard", results[0].Name);
    }

    [Fact]
    public void OutOfStockProducts_ExposeBadgeMetadata()
    {
        var product = new Product
        {
            Name = "Desk Mat",
            Category = "Accessories",
            Description = "Large desk mat",
            Stock = 0
        };

        Assert.True(product.IsOutOfStock);
        Assert.Equal("Out of Stock", product.AvailabilityBadgeText);
        Assert.Equal("badge bg-secondary", product.AvailabilityBadgeClass);
    }
}
