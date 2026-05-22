using CatalogApi.Models;
using CatalogApi.Services;

namespace CatalogApi.Tests;

public class ProductSearchServiceTests
{
    private readonly ProductSearchService _sut = new();

    private static List<Product> SampleProducts() =>
    [
        new() { Name = "Wireless Keyboard",  Category = "Electronics", Description = "Ergonomic keyboard", Price = 79.99m,  AvailableStock = 150 },
        new() { Name = "USB-C Monitor",       Category = "Electronics", Description = "27-inch 4K monitor", Price = 449.99m, AvailableStock = 45  },
        new() { Name = "Standing Desk",        Category = "Furniture",   Description = "Height-adjustable",  Price = 599.99m, AvailableStock = 0   },
        new() { Name = "Desk Lamp",            Category = "Furniture",   Description = "LED lamp",           Price = 34.99m,  AvailableStock = 200 },
        new() { Name = "Webcam HD",            Category = "Electronics", Description = "1080p webcam",       Price = 89.99m,  AvailableStock = 0   },
    ];

    // ── Search term filtering ────────────────────────────────────────────────

    [Fact]
    public void Search_NoQuery_ReturnsAllProducts()
    {
        var products = SampleProducts();
        var results = _sut.Search(products, null, hideOutOfStock: false).ToList();
        Assert.Equal(products.Count, results.Count);
    }

    [Fact]
    public void Search_EmptyQuery_ReturnsAllProducts()
    {
        var products = SampleProducts();
        var results = _sut.Search(products, "", hideOutOfStock: false).ToList();
        Assert.Equal(products.Count, results.Count);
    }

    [Fact]
    public void Search_QueryMatchesName_ReturnsMatchingProducts()
    {
        var products = SampleProducts();
        var results = _sut.Search(products, "keyboard", hideOutOfStock: false).ToList();
        Assert.Single(results);
        Assert.Equal("Wireless Keyboard", results[0].Name);
    }

    [Fact]
    public void Search_QueryMatchesCategory_ReturnsMatchingProducts()
    {
        var products = SampleProducts();
        var results = _sut.Search(products, "electronics", hideOutOfStock: false).ToList();
        Assert.Equal(3, results.Count);
    }

    [Fact]
    public void Search_QueryMatchesDescription_ReturnsMatchingProducts()
    {
        var products = SampleProducts();
        var results = _sut.Search(products, "4K", hideOutOfStock: false).ToList();
        Assert.Single(results);
        Assert.Equal("USB-C Monitor", results[0].Name);
    }

    [Fact]
    public void Search_QueryIsCaseInsensitive()
    {
        var products = SampleProducts();
        var results = _sut.Search(products, "KEYBOARD", hideOutOfStock: false).ToList();
        Assert.Single(results);
    }

    [Fact]
    public void Search_QueryNoMatch_ReturnsEmpty()
    {
        var products = SampleProducts();
        var results = _sut.Search(products, "zzznomatch", hideOutOfStock: false).ToList();
        Assert.Empty(results);
    }

    // ── Out-of-stock filtering ───────────────────────────────────────────────

    [Fact]
    public void Search_HideOutOfStock_ExcludesZeroStockProducts()
    {
        var products = SampleProducts();
        var results = _sut.Search(products, null, hideOutOfStock: true).ToList();
        Assert.All(results, p => Assert.True(p.AvailableStock > 0));
    }

    [Fact]
    public void Search_HideOutOfStock_CountIsCorrect()
    {
        var products = SampleProducts();
        var inStockCount = products.Count(p => p.AvailableStock > 0);
        var results = _sut.Search(products, null, hideOutOfStock: true).ToList();
        Assert.Equal(inStockCount, results.Count);
    }

    [Fact]
    public void Search_ShowOutOfStock_IncludesZeroStockProducts()
    {
        var products = SampleProducts();
        var results = _sut.Search(products, null, hideOutOfStock: false).ToList();
        Assert.Contains(results, p => p.AvailableStock == 0);
    }

    // ── Sorting ──────────────────────────────────────────────────────────────

    [Fact]
    public void Search_InStockItemsAppearBeforeOutOfStockItems()
    {
        var products = SampleProducts();
        var results = _sut.Search(products, null, hideOutOfStock: false).ToList();

        // Find indices of first out-of-stock and last in-stock item
        var firstOutOfStockIndex = results.FindIndex(p => p.AvailableStock == 0);
        var lastInStockIndex = results.FindLastIndex(p => p.AvailableStock > 0);

        Assert.True(firstOutOfStockIndex > lastInStockIndex,
            "All in-stock items should appear before all out-of-stock items.");
    }

    [Fact]
    public void Search_OutOfStockItemsAtBottom_WhenQueryApplied()
    {
        var products = SampleProducts();
        // "desk" matches "Standing Desk" (out of stock) and "Desk Lamp" (in stock)
        var results = _sut.Search(products, "desk", hideOutOfStock: false).ToList();
        Assert.Equal(2, results.Count);
        Assert.Equal("Desk Lamp", results[0].Name);
        Assert.Equal("Standing Desk", results[1].Name);
    }

    // ── Combined filter + sort ───────────────────────────────────────────────

    [Fact]
    public void Search_HideOutOfStockAndQuery_ReturnsOnlyMatchingInStockItems()
    {
        var products = SampleProducts();
        // "monitor" matches USB-C Monitor (in stock); no out-of-stock monitors
        var results = _sut.Search(products, "monitor", hideOutOfStock: true).ToList();
        Assert.Single(results);
        Assert.True(results[0].AvailableStock > 0);
    }
}
