var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var products = new[]
{
    new Product(1, "Keyboard", 45m),
    new Product(2, "Mouse", 20m),
    new Product(3, "Monitor", 180m),
};

// Root: shows which version (commit) is running.
app.MapGet("/", () => new
{
    message = "Hello from CI/CD demo",
    version = Environment.GetEnvironmentVariable("GIT_SHA") ?? "dev",
    hostname = Environment.MachineName
});

// Used by the Docker HEALTHCHECK.
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/products", () => products);

app.MapGet("/api/products/{id:int}", (int id) =>
    products.FirstOrDefault(p => p.Id == id) is { } product
        ? Results.Ok(product)
        : Results.NotFound(new { error = "Product not found" }));

app.Run();

public record Product(int Id, string Name, decimal Price);

// Makes the entry point visible to the test project (WebApplicationFactory<Program>).
public partial class Program { }
