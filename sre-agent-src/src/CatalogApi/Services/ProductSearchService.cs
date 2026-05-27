using CatalogApi.Models;

namespace CatalogApi.Services;

public class ProductSearchService
{
    public IEnumerable<Product> ApplySearch(IEnumerable<Product> products, string? search, bool hideOutOfStock)
    {
        var results = products.Where(product =>
            string.IsNullOrWhiteSpace(search)
            || product.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
            || product.Category.Contains(search, StringComparison.OrdinalIgnoreCase)
            || product.Description.Contains(search, StringComparison.OrdinalIgnoreCase));

        if (hideOutOfStock)
        {
            results = results.Where(product => !product.IsOutOfStock);
        }

        return results
            .OrderBy(product => product.IsOutOfStock)
            .ThenBy(product => product.Name, StringComparer.OrdinalIgnoreCase);
    }
}
