using CatalogApi.Models;

namespace CatalogApi.Services;

public class ProductSearchService
{
    /// <summary>
    /// Searches and filters a collection of products based on an optional search term and
    /// an optional flag to hide out-of-stock items. Out-of-stock products are sorted to
    /// the bottom of the results.
    /// </summary>
    /// <param name="products">The full list of products to search.</param>
    /// <param name="query">
    /// Optional search term matched case-insensitively against product name,
    /// description, and category. Pass <c>null</c> or empty to return all products.
    /// </param>
    /// <param name="hideOutOfStock">
    /// When <c>true</c>, products with <see cref="Product.AvailableStock"/> of 0 or less
    /// are excluded from the results entirely.
    /// </param>
    /// <returns>
    /// Filtered products with in-stock items first, out-of-stock items at the bottom.
    /// </returns>
    public IEnumerable<Product> Search(IEnumerable<Product> products, string? query, bool hideOutOfStock)
    {
        var results = products.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            results = results.Where(p =>
                p.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                p.Description.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                p.Category.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (hideOutOfStock)
        {
            results = results.Where(p => p.AvailableStock > 0);
        }

        // In-stock items first, out-of-stock at the bottom
        return results.OrderByDescending(p => p.AvailableStock > 0);
    }
}
