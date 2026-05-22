using CatalogApi.Models;
using CatalogApi.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Services
// ---------------------------------------------------------------------------
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<CosmosDbService>();
builder.Services.AddSingleton<OrderValidationService>();
builder.Services.AddSingleton<ProductSearchService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalog API v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "Catalog API - Swagger UI";
});

// ---------------------------------------------------------------------------
// Cosmos DB initialization (non-blocking — app starts even if DB is unreachable)
// ---------------------------------------------------------------------------
app.Lifetime.ApplicationStarted.Register(() =>
{
    _ = Task.Run(async () =>
    {
        try
        {
            var cosmosDb = app.Services.GetRequiredService<CosmosDbService>();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            await cosmosDb.InitializeAsync().WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            app.Logger.LogWarning(
                "Cosmos DB initialization timed out during background startup. The app remains online and will retry on request path.");
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex,
                "Cosmos DB initialization failed in background. The app remains online, but database requests may return errors.");
        }
    });
});

// ---------------------------------------------------------------------------
// Endpoints
// ---------------------------------------------------------------------------

// Home page - quick status and navigation links for key API endpoints
app.MapGet("/", () =>
{
        const string html = """
<!doctype html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Catalog API</title>
    <style>
        body { font-family: Segoe UI, Arial, sans-serif; margin: 2rem; line-height: 1.5; }
        h1 { margin-bottom: .25rem; }
        p { color: #333; }
        ul { padding-left: 1.25rem; }
        code { background: #f2f2f2; padding: .15rem .35rem; border-radius: 4px; }
        .card { max-width: 760px; border: 1px solid #ddd; border-radius: 10px; padding: 1rem 1.25rem; }
        .search-section { max-width: 760px; margin-top: 1.5rem; border: 1px solid #ddd; border-radius: 10px; padding: 1rem 1.25rem; }
        .search-bar { display: flex; gap: .5rem; flex-wrap: wrap; align-items: center; margin-bottom: 1rem; }
        .search-bar input[type=text] { flex: 1; min-width: 200px; padding: .4rem .6rem; border: 1px solid #ccc; border-radius: 6px; font-size: 1rem; }
        .search-bar button { padding: .4rem .9rem; background: #0078d4; color: #fff; border: none; border-radius: 6px; cursor: pointer; font-size: 1rem; }
        .search-bar button:hover { background: #005a9e; }
        .filter-row { font-size: .9rem; color: #555; display: flex; align-items: center; gap: .4rem; }
        .product-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(200px, 1fr)); gap: 1rem; margin-top: 1rem; }
        .product-card { border: 1px solid #ddd; border-radius: 8px; padding: .75rem; position: relative; background: #fff; }
        .product-card h3 { margin: 0 0 .25rem; font-size: 1rem; }
        .product-card .category { font-size: .8rem; color: #888; margin-bottom: .3rem; }
        .product-card .price { font-weight: bold; color: #0078d4; }
        .out-of-stock-badge { position: absolute; top: .5rem; right: .5rem; text-transform: uppercase; letter-spacing: .05em; }
        .product-card.out-of-stock { opacity: .7; }
        #search-results-info { font-size: .9rem; color: #555; margin-bottom: .5rem; }
        #no-results { display: none; color: #666; font-style: italic; }
    </style>
</head>
<body>
    <div class="card">
        <h1>Catalog API is running</h1>
        <p>Use the links below to test core endpoints.</p>
        <ul>
            <li><a href="/health">GET /health</a> - service and Cosmos connectivity check</li>
            <li><a href="/products">GET /products</a> - list products</li>
            <li><a href="/products/search">GET /products/search</a> - search products (supports <code>q</code> and <code>hideOutOfStock</code> params)</li>
            <li><a href="/orders">GET /orders</a> - list orders</li>
            <li><a href="/swagger">Swagger UI</a> - interactive API explorer</li>
            <li><a href="/swagger/v1/swagger.json">OpenAPI JSON</a> - API schema</li>
        </ul>
        <p>Write endpoints:</p>
        <ul>
            <li><code>POST /products</code></li>
            <li><code>POST /orders</code></li>
        </ul>
    </div>

    <div class="search-section">
        <h2 style="margin-top:0">Product Search</h2>
        <div class="search-bar">
            <input type="text" id="search-input" placeholder="Search products…" aria-label="Search products" />
            <button onclick="runSearch()">Search</button>
        </div>
        <div class="filter-row">
            <input type="checkbox" id="hide-oos" onchange="runSearch()" />
            <label for="hide-oos">Hide out-of-stock items</label>
        </div>
        <p id="search-results-info"></p>
        <p id="no-results">No products found.</p>
        <div class="product-grid" id="product-grid"></div>
    </div>

    <script>
        async function runSearch() {
            const q = document.getElementById('search-input').value;
            const hideOos = document.getElementById('hide-oos').checked;
            const params = new URLSearchParams();
            if (q) params.set('q', q);
            if (hideOos) params.set('hideOutOfStock', 'true');

            const res = await fetch('/products/search?' + params.toString());
            if (!res.ok) { console.error('Search failed', res.status); return; }
            const products = await res.json();

            const grid = document.getElementById('product-grid');
            const info = document.getElementById('search-results-info');
            const noResults = document.getElementById('no-results');
            grid.innerHTML = '';

            if (products.length === 0) {
                noResults.style.display = '';
                info.textContent = '';
                return;
            }
            noResults.style.display = 'none';
            info.textContent = products.length + ' product(s) found';

            products.forEach(p => {
                const inStock = p.availableStock > 0;
                const card = document.createElement('div');
                card.className = 'product-card' + (inStock ? '' : ' out-of-stock');
                card.innerHTML = `
                    ${!inStock ? '<span class="badge bg-secondary out-of-stock-badge">Out of Stock</span>' : ''}
                    <h3>${escHtml(p.name)}</h3>
                    <div class="category">${escHtml(p.category)}</div>
                    <div>${escHtml(p.description)}</div>
                    <div class="price">$${p.price.toFixed(2)}</div>
                    ${inStock ? '<div style="font-size:.8rem;color:#107c10">In stock: ' + p.availableStock + '</div>' : ''}
                `;
                grid.appendChild(card);
            });
        }

        function escHtml(str) {
            return String(str).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
        }

        // Load all products on page load
        runSearch();
    </script>
</body>
</html>
""";

        return Results.Content(html, "text/html");
});

// Health check — validates Cosmos DB connectivity
app.MapGet("/health", async (CosmosDbService db) =>
{
    var healthy = await db.CheckHealthAsync();
    return healthy
        ? Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow })
        : Results.Json(new { status = "unhealthy", timestamp = DateTime.UtcNow }, statusCode: 503);
});

// GET /products — list all products
app.MapGet("/products", async (CosmosDbService db) =>
{
    var products = await db.GetProductsAsync();
    return Results.Ok(products);
});

// GET /products/search — search products with optional query and out-of-stock filter
app.MapGet("/products/search", async (
    string? q,
    bool hideOutOfStock,
    CosmosDbService db,
    ProductSearchService searchService) =>
{
    var all = await db.GetProductsAsync();
    var results = searchService.Search(all, q, hideOutOfStock);
    return Results.Ok(results);
});

// GET /products/{id} — get a single product by id
app.MapGet("/products/{id}", async (string id, CosmosDbService db) =>
{
    var product = await db.GetProductByIdAsync(id);
    return product is not null ? Results.Ok(product) : Results.NotFound();
});

// POST /products — create a new product
app.MapPost("/products", async (Product product, CosmosDbService db) =>
{
    var created = await db.CreateProductAsync(product);
    return Results.Created($"/products/{created.Id}", created);
});

// GET /orders — list all orders
app.MapGet("/orders", async (CosmosDbService db) =>
{
    var orders = await db.GetOrdersAsync();
    return Results.Ok(orders);
});

// GET /orders/{id} — get a single order by id
app.MapGet("/orders/{id}", async (string id, CosmosDbService db) =>
{
    var order = await db.GetOrderByIdAsync(id);
    return order is not null ? Results.Ok(order) : Results.NotFound();
});

// POST /orders — create a new order (can trigger CPU spike in strict mode)
app.MapPost("/orders", async (Order order, CosmosDbService db, OrderValidationService validator) =>
{
    var validation = validator.ValidateOrder(order);
    if (!validation.IsValid)
        return Results.BadRequest(new { error = validation.Error });

    var created = await db.CreateOrderAsync(order);
    return Results.Created($"/orders/{created.Id}", created);
});

app.Run();
