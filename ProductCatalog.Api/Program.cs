
using Microsoft.EntityFrameworkCore;
using ProductCatalog.Api.Data;
using ProductCatalog.Api.Models;

var builder = WebApplication.CreateBuilder(args);

// SQL Server connection
var connectionString = builder.Configuration
    .GetConnectionString("CatalogDbConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// Swagger configuration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Enable Swagger
app.UseSwagger();
app.UseSwaggerUI();

// Create database if it does not exist
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<AppDbContext>();

    db.Database.EnsureCreated();
}

// Health check endpoint
app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        status = "Healthy",
        timestamp = DateTime.UtcNow
    });
})
.WithName("HealthCheck");

// Get product by ID
app.MapGet("/api/get-product/{id:int}", async (int id, AppDbContext db) =>
{
    var product = await db.Products
        .AsNoTracking()
        .FirstOrDefaultAsync(p => p.Id == id);

    if (product is null)
    {
        return Results.NotFound(new
        {
            message = $"Aucun produit trouvé avec l'identifiant {id}."
        });
    }

    return Results.Ok(product);
})
.WithName("GetProductById");

// Get all products
app.MapGet("/api/get-all-products", async (AppDbContext db) =>
{
    var products = await db.Products
        .AsNoTracking()
        .ToListAsync();

    return Results.Ok(products);
})
.WithName("GetAllProducts");

// Create product
app.MapPost("/api/create-product", async (Product product, AppDbContext db) =>
{
    db.Products.Add(product);
    await db.SaveChangesAsync();

    return Results.Created(
        $"/api/get-product/{product.Id}",
        product
    );
})
.WithName("CreateProduct");

app.Run();
